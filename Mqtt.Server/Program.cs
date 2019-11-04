using System.Net.Mqtt.Server.Hosting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mqtt.Server
{
    internal class Program
    {
        private static Task Main(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(ConfigureHost)
                .ConfigureWebHost(ConfigureWebHost)
                .ConfigureMqttService(o => {})
                .UseWindowsService()
                .UseSystemd()
                .Build()
                .RunAsync();
        }

        private static void ConfigureHost(IConfigurationBuilder builder)
        {
            builder.AddEnvironmentVariables("MQTT_");
        }

        private static void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .ConfigureAppConfiguration((ctx, wb) => wb.AddEnvironmentVariables("MQTT_KESTREL_"))
                .UseStartup<Startup>()
                .UseKestrel();
        }
    }
}