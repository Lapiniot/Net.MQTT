using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Mqtt.Server.Identity;
using Mqtt.Server.Web;
using OOs.Extensions.Hosting;
using OOs.Reflection;
using static System.Environment;

Console.WriteLine();
Console.Write("\e[38;5;105m");
Console.WriteLine("""

 ██████   █████ ██████   ██████    ██████    ███████████ ███████████
░░██████ ░░███ ░░██████ ██████   ███░░░░███ ░█░░░███░░░█░█░░░███░░░█
 ░███░███ ░███  ░███░█████░███  ███    ░░███░   ░███  ░ ░   ░███  ░ 
 ░███░░███░███  ░███░░███ ░███ ░███     ░███    ░███        ░███    
 ░███ ░░██████  ░███ ░░░  ░███ ░███   ██░███    ░███        ░███    
 ░███  ░░█████  ░███      ░███ ░░███ ░░████     ░███        ░███    
 █████  ░░█████ █████     █████ ░░░██████░██    █████       █████   
░░░░░    ░░░░░ ░░░░░     ░░░░░    ░░░░░░ ░░    ░░░░░       ░░░░░    

""");
Console.WriteLine(Assembly.GetEntryAssembly().BuildLogoString());
Console.Write("\e[39m\e[22m");
Console.WriteLine();

var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions() { Args = args, ApplicationName = "mqtt-server" });

builder.AddServiceDefaults();

#region Host configuration

var userConfigDir = builder.Environment.GetAppConfigPath();
var userConfigPath = Path.Combine(userConfigDir, "appsettings.json");
var runningInContainer = bool.TryParse(GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var value) && value;

if (runningInContainer)
{
    var exampleConfigPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.Production.json.distrib");
    if (Path.Exists(exampleConfigPath))
    {
        Directory.CreateDirectory(userConfigDir);
        File.Copy(exampleConfigPath, $"{userConfigPath}.distrib", true);
    }
}

builder.Configuration
    .AddJsonFile(userConfigPath, true, true)
    .AddEnvironmentVariables("MQTT_");

#endregion

#region Configure Logging, Metrics reporting and OpenTelemetry

// Override hardcoded FormatterName = "simple" enforcement done by WebApplication.CreateSlimBuilder() here 
// https://github.com/dotnet/runtime/blob/81dff0439effb8dabb62421904cdcea8f26c8f0f/src/libraries/Microsoft.Extensions.Logging.Console/src/ConsoleLoggerExtensions.cs#L61-L66
// and allow dynamic console logger configuration as 'fat' WebApplication.CreateBuilder() does by default!!!
builder.Services.AddTransient<IConfigureOptions<ConsoleLoggerOptions>>(static sp =>
    new ConfigureNamedOptions<ConsoleLoggerOptions, ILoggerProviderConfiguration<ConsoleLoggerProvider>>(
        name: Options.DefaultName,
        dependency: sp.GetRequiredService<ILoggerProviderConfiguration<ConsoleLoggerProvider>>(),
        action: static (options, provider) => provider.Configuration.Bind(options)));

builder.Metrics.AddConfiguration(builder.Configuration.GetSection("Metrics"));
builder.Services.AddOpenTelemetry().WithMetrics(mpb => mpb.AddMeter("Net.Mqtt.Server"));

#endregion

#region ASP.NET general purpose services and middleware configuration (Caching, Request Timeouts, Health-checks etc.)

builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy("HealthChecks",
            build: pb => pb.AddPolicy<AllowCachingAuthenticatedPolicy>().Expire(TimeSpan.FromSeconds(10)),
            excludeDefaultPolicy: true);

        options.AddPolicy("NoWebUIFallback",
            build: pb => pb.AddPolicy<AllowCachingAuthenticatedPolicy>().Expire(TimeSpan.FromMinutes(5)),
            excludeDefaultPolicy: true);
    });

builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("HealthChecks", TimeSpan.FromSeconds(2));
    options.AddPolicy("NoWebUIFallback", TimeSpan.FromSeconds(2));
});

builder.Services.AddConnections();

builder.Services.AddHealthChecks().AddMemoryCheck();

#endregion

#region Kestrel HTTPS configuration with MQTT integration enabled

builder.WebHost
    .UseKestrelHttpsConfiguration()
    .ConfigureKestrel(kso => kso.ConfigureEndpointDefaults(listenOptions =>
    {
        // Cleanup existing Unix Domain Socket files before running endpoints on them
        if (listenOptions is { SocketPath: { } path } && File.Exists(path))
        {
            File.Delete(path);
        }
    }))
    .UseQuic(options =>
    {
        // Configure server defaults to match client defaults.
        options.DefaultStreamErrorCode = 0x10c; // H3_REQUEST_CANCELLED (0x10C)
        options.DefaultCloseErrorCode = 0x100; // H3_NO_ERROR (0x100)
    })
    .UseMqtt();

#endregion

#region MQTT server configuration

builder.Services.AddMqttServer();
//builder.Services.AddMqttAuthentication((userName, passwd) => true);
builder.Host.ConfigureMqttServer((ctx, builder) => builder.UseHttpServerWebSocketConnections());

#endregion

#region Authorization / Authentication

var defaultScheme = RuntimeOptions.WebUISupported ? IdentityConstants.ApplicationScheme : "";
builder.Services.AddAuthentication(defaultScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;
    })
    .AddCertificateCache()
    .AddJwtBearer()
    .AddIdentityCookies();

#endregion

if (RuntimeOptions.WebUISupported)
{
    var connectionString = builder.Configuration.GetConnectionString("SqliteAppDbContextConnection")
        ?? throw new InvalidOperationException("Connection string 'SqliteAppDbContextConnection' not found.");

    var dataDir = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
    if (!string.IsNullOrEmpty(dataDir))
    {
        Directory.CreateDirectory(dataDir);
    }

    builder.Services.AddMqttServerIdentity()
        .AddMqttServerIdentityStore(options => options
            .UseModel(Mqtt.Server.Identity.Sqlite.Compiled.ApplicationDbContextModel.Instance)
            .UseSqlite(connectionString,
                options => options.MigrationsAssembly(typeof(Mqtt.Server.Identity.Sqlite.Compiled.ApplicationDbContextModel).Assembly)));

    builder.Services.AddMqttServerUI();

    if (builder.Environment.IsDevelopment())
    {
        builder.WebHost.UseStaticWebAssets();
    }
}

if (!runningInContainer)
{
    if (OperatingSystem.IsLinux())
    {
        builder.Host.UseSystemd();
    }
    else if (OperatingSystem.IsWindows())
    {
        builder.Host.UseWindowsService();
    }
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();
app.UseOutputCache();
app.UseRequestTimeouts();

app.MapMqttWebSockets();

var healthChecks = app.MapGroup("/healthz");
healthChecks.CacheOutput("HealthChecks").WithRequestTimeout("HealthChecks");
healthChecks.MapHealthChecks("", new() { Predicate = check => check.Tags.Count == 0 });
healthChecks.MapMemoryHealthCheck("memory");

if (RuntimeOptions.WebUISupported)
{
    app.UseAntiforgery();
    app.UseAuthorization();
#if NET9_0_OR_GREATER
    app.MapStaticAssets();
#else
    app.UseStaticFiles();
#endif
    app.MapMqttServerUI();
}
else
{
    app.Map("/", async ctx =>
    {
        await ctx.Response.WriteAsync("""
        <!DOCTYPE html>
        <html lang="en">
        <body>
            <h3>WebUI feature is not supported by this server instance.</h3>
        </body>
        </html>
        """, ctx.RequestAborted).ConfigureAwait(false);
        await ctx.Response.CompleteAsync().ConfigureAwait(false);
    })
    .CacheOutput("NoWebUIFallback")
    .WithRequestTimeout("NoWebUIFallback");
}

await CertificateGenerateInitializer.InitializeAsync(builder.Environment, builder.Configuration, CancellationToken.None).ConfigureAwait(false);

if (RuntimeOptions.WebUISupported)
{
    await InitializeIdentityExtensions.InitializeIdentityStoreAsync(app.Services).ConfigureAwait(false);
}

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);