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
                .ConfigureHostConfiguration(b => b.AddEnvironmentVariables("MQTT_"))
                .ConfigureWebHost(b => b
                    .ConfigureAppConfiguration((ctx, cb) => cb.AddEnvironmentVariables("MQTT_KESTREL_"))
                    .UseStartup<Startup>()
                    .UseKestrel((hbc, o) => o.Configure(hbc.Configuration.GetSection("Kestrel"))))
                .ConfigureMqttService()
                .UseMqttService()
                .UseWindowsService()
                .UseSystemd()
                .Build()
                .RunAsync();
        }
    }
}