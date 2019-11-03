﻿using System.Configuration;
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
        private static void Main(string[] args)
        {
             Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cb) => cb
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true))
                .ConfigureHostConfiguration(cb => cb
                    .AddEnvironmentVariables("MQTT_")
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
                .Run();
        }
    }
}