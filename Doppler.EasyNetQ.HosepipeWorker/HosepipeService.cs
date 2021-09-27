using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Doppler.Extensions.Logging;
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
                var bus = _busStation.GetBus(service.Name);
                bus.SubscribeAsync<Error>(
                    subscriptionId: string.Empty,
                    onMessage: async (error) => await RepublishErrorAsync(service.Name, error),
                    configure: (c) => c.WithQueueName(_hosepipeSettings.ErrorQueueName)
                    );

                _logger.LogInformation("Subscribe to queue {QueueName} of service {Service}", _hosepipeSettings.ErrorQueueName, service.Name);
            }
        }

        private async Task ConsumeServicesErrorQueue()
        {
            foreach (var service in _hosepipeSettings.Connections)
            {
                _logger.LogInformation("Consuming errores from queue {QueueName} of service {Service}", _hosepipeSettings.ErrorQueueName, service.Name);

                var errorMessages = GetErrorsFromQueue(service.Name);

                foreach (var errorMessage in errorMessages)
                {
                    await RepublishErrorAsync(service.Name, errorMessage);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogServiceStop(this);

            await base.StopAsync(cancellationToken);
        }

        public List<Error> GetErrorsFromQueue(string service)
        {
            var errorList = new List<Error>();

            while (errorList.Count < _hosepipeSettings.NumberOfMessagesToRetrieve)
            {
                var errorMessage = _busStation.GetBus(service).Advanced.Get<Error>(new Queue(_hosepipeSettings.ErrorQueueName, isExclusive: false));

                if (errorMessage == null) break; // no more messages on the queue

                errorList.Add(errorMessage.Message.Body);
            }

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

                error.BasicProperties.Headers[_hosepipeSettings.RetryCountHeader] = (int)retryCount + 1;

                if ((int)error.BasicProperties.Headers[_hosepipeSettings.RetryCountHeader] > _hosepipeSettings.MaxRetryCount)
                {
                    await _busStation.GetBus(service).PublishAsync(error, configure => configure.WithQueueName(_hosepipeSettings.UnsolvedErrorQueueName));

                    _logger.LogInformation("Max retry reached, error published to {QueueName}", _hosepipeSettings.UnsolvedErrorQueueName);

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
                    await _busStation.GetBus(service).PublishAsync(error, configure => configure.WithQueueName(_hosepipeSettings.ErrorQueueName));
                    _logger.LogWarning(ex, "Unexpected problem publishing message to the original queue, error message was published again to retry later");
                }
                catch (Exception republishErrorException)
                {
                    _logger.LogError(
                        republishErrorException,
                        "Unexpected problem republishing message error to the error queue, the message was lost. @{ErrorMessage}",
                        error);
                }
            }
        }
    }
}
