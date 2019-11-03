using System.Configuration;
using System.Net.Mqtt.Server.Hosting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(configure => configure
                    .UseStartup<Startup>()
                    .UseKestrel()
                    .ConfigureAppConfiguration((ctx, wb) =>
                    {
                        wb.AddEnvironmentVariables("MQTT_KESTREL_");
                    }))
                .ConfigureAppConfiguration((ctx, cb) => cb
                   .AddJsonFile("appsettings.json", false)
                   .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true))
                .ConfigureHostConfiguration(cb => cb
                   .AddCommandLine(args)
                   .AddJsonFile("settings.mqtt.json", true))
                .ConfigureMqttService(o => { })
                .ConfigureLogging(lb => lb
                   .SetMinimumLevel(Trace)
                   .AddConsole()
                   .AddDebug())
                .UseWindowsService()
                .UseSystemd()
                .Build()
                .RunAsync();
        }
    }
}