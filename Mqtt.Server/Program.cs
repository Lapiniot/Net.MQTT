using System.Configuration;
using System.Net.Mqtt.Server.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Logging.LogLevel;

namespace Mqtt.Server
{
    internal class Program
    {
        private static Task Main(string[] args)
        {
            return new HostBuilder()
                .ConfigureAppConfiguration((ctx, cb) => cb
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true)
                    .AddCommandArguments(args))
                .ConfigureHostConfiguration(cb => cb
                    .AddEnvironmentVariables("MQTT_")
                    .AddJsonFile("settings.mqtt.json", true))
                .ConfigureMqttService(o => {})
                .ConfigureLogging(lb => lb
                    .SetMinimumLevel(Information)
                    .AddConsole()
                    .AddDebug())
                .RunConsoleAsync();
        }
    }
}