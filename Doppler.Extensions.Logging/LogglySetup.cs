using System;
using Loggly;
using Loggly.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Doppler.Extensions.Logging
{
    public static class LogglySetup
    {
        private const string DefaultEndpointHostname = "logs-01.loggly.com";
        private const int DefaultEndpointPort = 443;

        public static IConfiguration ConfigureLoggly(
            this IConfiguration configuration,
            IHostEnvironment hostingEnvironment,
            string appSettingsSection = nameof(LogglyConfig))
        {
            var config = LogglyConfig.Instance;

            // Set default values
            config.ThrowExceptions = true;
            config.Transport.EndpointPort = DefaultEndpointPort;

            // Bind values from configuration
            configuration.GetSection(appSettingsSection).Bind(config);

            // Configure convention values if not set in configuration
            config.ApplicationName ??= hostingEnvironment.ApplicationName;
            config.Transport.EndpointHostname ??= DefaultEndpointHostname;

            // Define custom tags for sent to Loggly
            config.TagConfig.Tags.AddRange(new ITag[]
            {
                new ApplicationNameTag {Formatter = "Application {0}"},
                new HostnameTag { Formatter = "Host {0}" },
                new SimpleTag { Value = $"Environment {hostingEnvironment.EnvironmentName}" },
                new SimpleTag { Value = $"Runtime {Environment.Version}" },
                new SimpleTag { Value = $"Platform {Environment.OSVersion.Platform}" },
                new SimpleTag { Value = $"OSVersion {Environment.OSVersion}" },
            });

            return configuration;
        }
    }
}
