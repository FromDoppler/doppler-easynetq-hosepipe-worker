using System.Collections.Generic;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.EasyNetQ.HosepipeWorker
{
    public class BusStation : IBusStation
    {
        private readonly ILogger<BusStation> _logger;
        private readonly IDictionary<string, IBus> _buses;

        public BusStation(ILogger<BusStation> logger, IOptions<HosepipeSettings> options)
        {
            _logger = logger;
            _buses = new Dictionary<string, IBus>();

            foreach (var connection in options.Value.Connections)
            {
                try
                {
                    var connectionConfiguration = new ConnectionStringParser().Parse(connection.Value.ConnectionString);
                    if (!string.IsNullOrWhiteSpace(connection.Value.SecretPassword))
                    {
                        connectionConfiguration.Password = connection.Value.SecretPassword;
                    }
                    var bus = RabbitHutch.CreateBus(connectionConfiguration, x =>
                    {
                        if (connection.Value.EnableLegacyTypeNaming)
                            x.EnableLegacyTypeNaming();
                    });

                    bus.Advanced.QueueDeclare(options.Value.ErrorQueueName);
                    bus.Advanced.QueueDeclare(options.Value.UnsolvedErrorQueueName);

                    TryAddBus(connection.Key, bus);
                }
                catch (EasyNetQException ex)
                {
                    _logger.LogError(ex, "Can create connection bus for service {BusName}", connection.Key);
                }
            }
        }

        public IBus GetBus(string busName)
        {
            if (_buses.TryGetValue(busName, out IBus bus))
            {
                return bus;
            }

            return null;
        }

        private void TryAddBus(string busName, IBus bus)
        {
            if (!_buses.TryAdd(busName, bus))
            {
                _logger.LogError("Error adding {BusName} to buses list", busName);
            }
        }
    }
}
