using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.EntityFrameworkCore;
using Mqtt.Server.Identity;
using Mqtt.Server.Web;

#pragma warning disable CA1852 // False positive from roslyn analyzer

var builder = WebApplication.CreateBuilder(args);

#region Host configuration

builder.Configuration
    .AddJsonFile("config/appsettings.json", true, true)
    .AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", true, true);

#endregion

builder.Services.AddWebSocketInterceptor();
builder.Services.AddHealthChecks().AddMemoryCheck();

#region Authorization / Authentication

var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ??
                       throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

builder.Services
    .AddMqttServerIdentity()
    .AddMqttServerIdentityStore(options => options.UseSqlite(connectionString));

builder.Services.AddAuthentication()
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;
    })
    .AddCertificateCache()
    .AddJwtBearer();

#endregion

builder.Services.AddMqttServerUI();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapMqttServerUI();

app.UseWebSockets();
app.MapWebSocketInterceptor("/mqtt");

app.MapHealthChecks("/health", new() { Predicate = check => check.Tags.Count == 0 });
app.MapMemoryHealthCheck("/health/memory");

await app.Services.InitializeMqttServerIdentityStoreAsync().ConfigureAwait(false);

await app.RunAsync().ConfigureAwait(false);