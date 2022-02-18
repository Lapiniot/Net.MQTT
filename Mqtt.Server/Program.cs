using System.Net.Mqtt.Server.AspNetCore.Hosting.Configuration;
using System.Net.Mqtt.Server.Hosting;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

#pragma warning disable CA1812 // False positive from roslyn analyzer

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebSocketInterceptor();
builder.Services.AddHealthChecks().AddMemoryCheck();
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;
    })
    .AddCertificateCache();

builder.Host.ConfigureAppConfiguration((ctx, configuration) => configuration
    .AddJsonFile("config/appsettings.json", true, true)
    .AddJsonFile($"config/appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables("MQTT_"));

builder.Host.ConfigureMqttHost(mqtt => mqtt
    //.UseAuthentication<TestMqttAuthHandler>()
    .AddWebSocketInterceptorListener());

if (OperatingSystem.IsLinux())
{
    builder.Host.UseSystemd();
}
else if (OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService();
}

var app = builder.Build();

app.UseAuthentication();
app.UseWebSockets();

app.MapWebSocketInterceptor("/mqtt");
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Count == 0 });
app.MapMemoryHealthCheck("/health/memory");

await app.RunAsync().ConfigureAwait(false);