using System;
using System.Configuration;
using System.Net.Mqtt;
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
            FastPacketIdPool p = new FastPacketIdPool();

            Parallel.For(0, 0xFFFF, (i, s) => p.Rent());

            Parallel.For(1, 0xFFFF+1, (i, s) => p.Return((ushort)i));

            Parallel.For(0, 0xFFFF, (i, s) => Console.Write(p.Rent() + ", "));

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