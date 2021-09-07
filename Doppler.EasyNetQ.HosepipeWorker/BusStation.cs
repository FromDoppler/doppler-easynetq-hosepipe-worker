using System.Collections.Generic;
using EasyNetQ;
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
                var bus = RabbitHutch.CreateBus(connection.ConnectionString, x => { });

                TryAddBus(connection.Name, bus);
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
