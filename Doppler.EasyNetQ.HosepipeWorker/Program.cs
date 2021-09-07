using Doppler.Extensions.Logging;
using EasyNetQ.Consumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Doppler.EasyNetQ.HosepipeWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostContext, loggerConfiguration) =>
                {
                    loggerConfiguration.SetupSeriLog(hostContext.Configuration, hostContext.HostingEnvironment);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    services.Configure<HosepipeSettings>(configuration.GetSection(nameof(HosepipeSettings)));
                    services.AddOptions();

                    services.AddSingleton<IErrorMessageSerializer, DefaultErrorMessageSerializer>();
                    services.AddSingleton<IBusStation, BusStation>();

                    services.AddHostedService<HosepipeService>();
                });
    }
}
