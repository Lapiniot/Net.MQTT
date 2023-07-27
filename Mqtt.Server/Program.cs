using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.EntityFrameworkCore;
using Mqtt.Server.Identity;
using Mqtt.Server.Web;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using static System.OperatingSystem;

Console.WriteLine();
Console.WriteLine(Assembly.GetEntryAssembly().BuildLogoString());
Console.WriteLine();

var builder = WebApplication.CreateBuilder(args);

#region Host configuration

if (IsWindows())
    builder.Configuration.AddJsonFile($"appsettings.Windows.json", true, true);
else if (IsLinux())
    builder.Configuration.AddJsonFile($"appsettings.Linux.json", true, true);
else if (IsFreeBSD())
    builder.Configuration.AddJsonFile($"appsettings.FreeBSD.json", true, true);
else if (IsMacOS() || IsMacCatalyst())
    builder.Configuration.AddJsonFile($"appsettings.MacOS.json", true, true);

builder.Configuration.AddJsonFile("config/appsettings.json", true, true);
builder.Configuration.AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", true, true);
builder.Configuration.AddEnvironmentVariables("MQTT_");

var useIdentitySupport = builder.Configuration.HasFlag("UseIdentitySupport");
var useAdminWebUI = builder.Configuration.HasFlag("UseAdminWebUI");

if (builder.Configuration.GetValue<bool?>("MetricsCollectionSupport") is { } isEnabled)
{
    AppContext.SetSwitch("System.Net.Mqtt.Server.MetricsCollectionSupport", isEnabled);
}

#endregion

builder.Services.AddWebSocketInterceptor();
builder.Services.AddHealthChecks().AddMemoryCheck();

#region Authorization / Authentication

if (useIdentitySupport)
{
    var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ??
        throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

    builder.Services
        .AddMqttServerIdentity()
        .AddMqttServerIdentityStore(options => options.UseSqlite(connectionString));
}

builder.Services.AddAuthentication()
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;
    })
    .AddCertificateCache()
    .AddJwtBearer();

#endregion

if (useAdminWebUI)
{
    builder.Services.AddMqttServerUI();
}

builder.Host.UseMqttServer()
    .ConfigureMqttServerOptions()
    //.AddMqttAuthentication((userName, passwd) => true)
    .AddWebSocketInterceptorListener();

if (IsLinux())
{
    builder.Host.UseSystemd();
}
else if (IsWindows())
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

if (useAdminWebUI)
{
    app.MapMqttServerUI();
}

if (useIdentitySupport)
{
    app.UseAuthorization();
    app.MapMqttServerIdentityUI();
}

app.UseWebSockets();
app.MapWebSocketInterceptor("/mqtt");

app.MapHealthChecks("/health", new() { Predicate = check => check.Tags.Count == 0 });
app.MapMemoryHealthCheck("/health/memory");

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "data"));

if (useIdentitySupport)
{
    await app.Services.InitializeMqttServerIdentityStoreAsync().ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);

internal static partial class Program
{
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    private static bool HasFlag(this IConfiguration configuration, string key) => configuration.GetValue(key, false);
}