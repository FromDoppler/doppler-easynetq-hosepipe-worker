using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Doppler.Extensions.Logging
{
    public static class BackgroundServiceExtensionsLogging
    {
        public static void LogServiceStart(this ILogger<BackgroundService> logger, BackgroundService service)
        {
            logger.LogInformation("Starting {Service} on {Host} running {OS} with runtime {Runtime}",
                service.GetType().Name,
                Environment.MachineName,
                Environment.OSVersion,
                Environment.Version);
        }

        public static void LogServiceStop(this ILogger<BackgroundService> logger, BackgroundService service)
        {
            logger.LogInformation("{Service} stoped on {Host}",
                service.GetType().Name,
                Environment.MachineName);
        }
    }
}
