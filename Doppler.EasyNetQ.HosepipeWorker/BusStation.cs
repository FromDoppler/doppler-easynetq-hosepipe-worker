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
        private readonly Dictionary<string, IBus> _buses;

        public BusStation(ILogger<BusStation> logger, IOptions<HosepipeSettings> options)
        {
            _logger = logger;
            _buses = new Dictionary<string, IBus>();

            foreach (var connection in options.Value.Connections)
            {
                try
                {
                    var connectionConfiguration = new ConnectionStringParser().Parse(connection.ConnectionString);
                    if (!string.IsNullOrWhiteSpace(connection.Password))
                    {
                        connectionConfiguration.Password = connection.Password;
                    }
                    var bus = RabbitHutch.CreateBus(connectionConfiguration, x => { });

                    TryAddBus(connection.Name, bus);
                }
                catch (EasyNetQException ex)
                {
                    _logger.LogError(ex, "Can create connection bus for service {BusName}", connection.Name);
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
