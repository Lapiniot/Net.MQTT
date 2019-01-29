using System;
using System.Configuration;
using System.Net;
using System.Net.Mqtt.Server.Hosting;
using System.Net.Mqtt.Server.Hosting.Configuration;
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
                .ConfigureAppConfiguration((ctx, cb) => cb
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true)
                    .AddCommandArguments(args))
                .ConfigureHostConfiguration(cb => cb
                    .AddEnvironmentVariables("MQTT_")
                    .AddJsonFile("settings.mqtt.json", true))
                .ConfigureMqttServer(o => o
                    .WithTcpEndpoint("tcp.default", new IPEndPoint(IPAddress.Loopback, 1883))
                    .WithWebSocketsEndpoint("ws.default", new Uri("http://localhost:8000/mqtt/"), "mqtt", "mqttv3.1"))
                .ConfigureLogging(lb => lb
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole()
                    .AddDebug())
                .RunConsoleAsync();
        }
    }
}