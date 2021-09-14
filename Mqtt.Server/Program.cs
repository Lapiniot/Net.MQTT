using System.Net.Mqtt.Server.AspNetCore.Hosting;
using System.Net.Mqtt.Server.Hosting;
using Mqtt.Server;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureWebHost(webBuilder => webBuilder
        .ConfigureAppConfiguration((ctx, configBuilder) => configBuilder.AddEnvironmentVariables("MQTT_KESTREL_"))
        .UseKestrel((ctx, options) => options.Configure(ctx.Configuration.GetSection("Kestrel"), true))
        .UseStartup<Startup>()
        .Configure(webApp => webApp.UseWebSocketInterceptor("/mqtt")))
    .ConfigureMqttHost(mqttBuilder => mqttBuilder
        //.UseAuthentication<TestMqttAuthHandler>()
        .UseWebSocketInterceptor()
        .ConfigureServices((ctx, services) => services.AddTransient<ICertificateValidationHandler, CertificateValidationHandler>()));

if(OperatingSystem.IsLinux())
{
    builder = builder.UseSystemd();
}
else if(OperatingSystem.IsWindows())
{
    builder = builder.UseWindowsService();
}

await builder
    .Build()
    .RunAsync()
    .ConfigureAwait(false);