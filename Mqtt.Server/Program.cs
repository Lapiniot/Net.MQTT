using System.Net.Mqtt.Server.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Mqtt.Server;

await Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(b => b.AddEnvironmentVariables("MQTT_"))
    .ConfigureWebHost(b => b
        .ConfigureAppConfiguration((ctx, cb) => cb.AddEnvironmentVariables("MQTT_KESTREL_"))
        .UseStartup<Startup>()
        .UseKestrel())
    .ConfigureMqttService()
    .UseMqttService()
    .UseWindowsService()
    .UseSystemd()
    .Build()
    .RunAsync()
    .ConfigureAwait(false);