using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Doppler.Extensions.Logging
{
    public static class SerilogSetup
    {
        public static LoggerConfiguration SetupSeriLog(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment)
        {
            configuration.ConfigureLoggly(hostEnvironment);

            loggerConfiguration
                .Enrich.WithProperty("Application", hostEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", hostEnvironment.EnvironmentName)
                .Enrich.WithProperty("Platform", Environment.OSVersion.Platform)
                .Enrich.WithProperty("Host", Environment.MachineName)
                .Enrich.FromLogContext();

            loggerConfiguration.ReadFrom.Configuration(configuration);

            return loggerConfiguration;
        }
    }
}
