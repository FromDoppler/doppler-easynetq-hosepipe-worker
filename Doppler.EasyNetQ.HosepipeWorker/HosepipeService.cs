using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Doppler.Extensions.Logging;
using EasyNetQ;
using EasyNetQ.Consumer;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace Doppler.EasyNetQ.HosepipeWorker
{
    public class HosepipeService : BackgroundService
    {
        private readonly ILogger<HosepipeService> _logger;
        private readonly IErrorMessageSerializer _errorMessageSerializer;
        private readonly IBusStation _busStation;
        private readonly HosepipeSettings _hosepipeSettings;

        public HosepipeService(
            ILogger<HosepipeService> logger,
            IErrorMessageSerializer errorMessageSerializer,
            IBusStation busStation,
            IOptions<HosepipeSettings> options)
        {
            _logger = logger;
            _errorMessageSerializer = errorMessageSerializer;
            _hosepipeSettings = options.Value;

            _busStation = busStation;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogServiceStart(this);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_hosepipeSettings.QueueProcessingStrategy == QueueProcessingStrategy.Subscribe)
            {
                SubscribeToTheServicesErrorQueue();
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_hosepipeSettings.QueueProcessingStrategy == QueueProcessingStrategy.Consume)
                {
                    await ConsumeServicesErrorQueue();
                }

                await Task.Delay(_hosepipeSettings.DelayedTimeInMilliseconds, cancellationToken);
            }
        }

        private void SubscribeToTheServicesErrorQueue()
        {
            foreach (var service in _hosepipeSettings.Connections)
            {
                var bus = _busStation.GetBus(service.Key);
                bus.PubSub.SubscribeAsync<Error>(
                    subscriptionId: string.Empty,
                    onMessage: async (error) => await RepublishErrorAsync(service.Key, error),
                    configure: (c) => c.WithQueueName(_hosepipeSettings.ErrorQueueName)
                    );

                _logger.LogInformation("Subscribe to queue {QueueName} of service {Service}", _hosepipeSettings.ErrorQueueName, service.Key);
            }
        }

        private async Task ConsumeServicesErrorQueue()
        {
            foreach (var service in _hosepipeSettings.Connections)
            {
                _logger.LogInformation("Consuming errores from queue {QueueName} of service {Service}", _hosepipeSettings.ErrorQueueName, service.Key);

                var errorMessages = await GetErrorsFromQueue(service.Key);

                foreach (var errorMessage in errorMessages)
                {
                    await RepublishErrorAsync(service.Key, errorMessage);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogServiceStop(this);

            await base.StopAsync(cancellationToken);
        }

        public async Task<IList<Error>> GetErrorsFromQueue(string service)
        {
            var pullingConsumer = _busStation.GetBus(service).Advanced.CreatePullingConsumer<Error>(new Queue(_hosepipeSettings.ErrorQueueName, isExclusive: false));

            var pullBatchResult = await pullingConsumer.PullBatchAsync(_hosepipeSettings.NumberOfMessagesToRetrieve);
            var errorList = pullBatchResult.Messages.Select(x => x.Message.Body).ToList();

            _logger.LogInformation("{Amount} error messages consumed and republished", errorList.Count);

            return errorList;
        }

        public async Task RepublishErrorAsync(string service, Error error)
        {
            try
            {
                if (!error.BasicProperties.Headers.TryGetValue(_hosepipeSettings.RetryCountHeader, out var retryCount))
                {
                    retryCount = 0;
                    error.BasicProperties.Headers.Add(_hosepipeSettings.RetryCountHeader, retryCount);
                }

                var retryCountValue = retryCount as int? ?? retryCount as long? ?? 0;
                retryCountValue++;

                error.BasicProperties.Headers[_hosepipeSettings.RetryCountHeader] = retryCountValue;

                if (retryCountValue > _hosepipeSettings.MaxRetryCount)
                {
                    _logger.LogWarning("Max retry reached");

                    await PublishErrorToUnsolvedQueue(service, error);

                    return;
                }

                var body = _errorMessageSerializer.Deserialize(error.Message);

                await _busStation.GetBus(service).Advanced.PublishAsync(new Exchange(error.Exchange), error.RoutingKey, mandatory: true, error.BasicProperties, body);
            }
            catch (OperationInterruptedException)
            {
                _logger.LogError("The exchange, '{Exchange}', described in the error message does not exist.'", error.Exchange);
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError(ex, "Unexpected problem publishing message to the original queue");

                    await PublishErrorToUnsolvedQueue(service, error);
                }
                catch (Exception republishErrorException)
                {
                    _logger.LogError(
                        republishErrorException,
                        "Unexpected problem publishing message error to the unsolved error queue, message discarted. @{ErrorMessage}",
                        error);
                }
            }
        }

        private async Task PublishErrorToUnsolvedQueue(string service, Error error)
        {
            error.BasicProperties.Headers[_hosepipeSettings.RetryCountHeader] = 0;

            await _busStation.GetBus(service).Advanced.PublishAsync(
                exchange: Exchange.GetDefault(),
                routingKey: _hosepipeSettings.UnsolvedErrorQueueName,
                mandatory: true,
                messageProperties: error.BasicProperties,
                body: new JsonSerializer().MessageToBytes(typeof(Error), error));

            _logger.LogInformation("Error published to {QueueName}", _hosepipeSettings.UnsolvedErrorQueueName);
        }
    }
}
