using System.Configuration;
using System.Net.Mqtt.Server.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mqtt.Server
{
    internal class Program
    {
        private static Task Main(string[] args)
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(cb => cb
                    .AddJsonFile("appsettings.json", false)
                    .AddCommandArguments(args))
                .ConfigureHostConfiguration(cb => cb
                    .AddEnvironmentVariables("MQTT_")
                    .AddJsonFile("settings.mqtt.json", true))
                .ConfigureMqttServer(options => {})
                .ConfigureLogging(lb => lb
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole()
                    .AddDebug())
                .RunConsoleAsync();
        }
    }
}