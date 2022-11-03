using System.Net.Mqtt.Server.AspNetCore.Hosting.Configuration;
using System.Net.Mqtt.Server.Hosting;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;

#pragma warning disable CA1812, CA1852 // False positive from roslyn analyzer

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

builder.Configuration
    .AddJsonFile("config/appsettings.json", true, true)
    .AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", true, true);

builder.Host.UseMqttServer()
    .ConfigureMqttServerDefaults()
    .AddWebSocketInterceptorListener();

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
app.MapHealthChecks("/health", new() { Predicate = check => check.Tags.Count == 0 });
app.MapMemoryHealthCheck("/health/memory");

await app.RunAsync().ConfigureAwait(false);