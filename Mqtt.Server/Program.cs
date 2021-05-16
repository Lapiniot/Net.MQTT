using System.Net.Mqtt.Server.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Mqtt.Server;

await Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(configBuilder => configBuilder.AddEnvironmentVariables("MQTT_"))
    .ConfigureWebHost(webBuilder => webBuilder
        .ConfigureAppConfiguration((ctx, configBuilder) => configBuilder.AddEnvironmentVariables("MQTT_KESTREL_"))
        .UseKestrel((ctx, options) => options.Configure(ctx.Configuration.GetSection("Kestrel"), true))
        .UseStartup<Startup>())
    //.ConfigureMqttService((ctx, services) => services.AddMqttAuthentication<TestMqttAuthHandler>())
    .ConfigureMqttService()
    .UseMqttService()
    .UseWindowsService()
    .UseSystemd()
    .Build()
    .RunAsync()
    .ConfigureAwait(false);