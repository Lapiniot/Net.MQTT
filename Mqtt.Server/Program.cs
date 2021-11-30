using System.Net.Mqtt.Server.AspNetCore.Hosting;
using System.Net.Mqtt.Server.Hosting;
using Mqtt.Server;

#pragma warning disable CA1812 // False positive from roslyn analyzer

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

builder.Host.ConfigureAppConfiguration(configuration => configuration.AddEnvironmentVariables("MQTTD_"));

builder.Host.ConfigureMqttHost(mqtt => mqtt
    //.UseAuthentication<TestMqttAuthHandler>()
    .UseWebSocketInterceptor()
    .ConfigureServices((ctx, services) => services.AddTransient<ICertificateValidationHandler, CertificateValidationHandler>()));

if(OperatingSystem.IsLinux())
{
    builder.Host.UseSystemd();
}
else if(OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService();
}

var app = builder.Build();

app.UseWebSockets();
app.MapWebSocketInterceptor("/mqtt");
app.MapHealthChecks("/health");

await app.RunAsync().ConfigureAwait(false);