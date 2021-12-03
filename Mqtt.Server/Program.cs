using System.Net.Mqtt.Server.AspNetCore.Hosting;
using System.Net.Mqtt.Server.Hosting;

#pragma warning disable CA1812 // False positive from roslyn analyzer

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddWebSocketInterceptor()
    .AddHealthChecks();

builder.Host.ConfigureAppConfiguration((ctx, configuration) => configuration
    .AddJsonFile("config/appsettings.json", true, true)
    .AddJsonFile($"config/appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables("MQTT_"));

builder.Host.ConfigureMqttHost(mqtt => mqtt
    //.UseAuthentication<TestMqttAuthHandler>()
    .AddWebSocketInterceptorListener());

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