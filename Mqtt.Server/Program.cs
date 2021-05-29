using System.Net.Mqtt.Server.AspNetCore.Hosting;
using System.Net.Mqtt.Server.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mqtt.Server;

await Host.CreateDefaultBuilder(args)
    .ConfigureWebHost(webBuilder => webBuilder
        .ConfigureAppConfiguration((ctx, configBuilder) => configBuilder.AddEnvironmentVariables("MQTT_KESTREL_"))
        .UseKestrel((ctx, options) => options.Configure(ctx.Configuration.GetSection("Kestrel"), true))
        .UseStartup<Startup>()
        .Configure(webApp => webApp.UseWebSocketInterceptor("/mqtt")))
    .ConfigureMqttHost(mqttBuilder => mqttBuilder
        //.UseAuthentication<TestMqttAuthHandler>()
        .UseWebSocketInterceptor()
        .ConfigureServices((ctx, services) => services.AddTransient<ICertificateValidationHandler, CertificateValidationHandler>()))
    .UseWindowsService()
    .UseSystemd()
    .Build()
    .RunAsync()
    .ConfigureAwait(false);