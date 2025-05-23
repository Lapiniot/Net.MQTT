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
using Mqtt.Server.Identity.Data.Compiled;
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

// Override hardcoded FormatterName = "simple" enforcement done by WebApplication.CreateSlimBuilder() here 
// https://github.com/dotnet/runtime/blob/81dff0439effb8dabb62421904cdcea8f26c8f0f/src/libraries/Microsoft.Extensions.Logging.Console/src/ConsoleLoggerExtensions.cs#L61-L66
// and allow dynamic console logger configuration as 'fat' WebApplication.CreateBuilder() does by default!!!
builder.Services.AddTransient<IConfigureOptions<ConsoleLoggerOptions>>(static sp =>
    new ConfigureNamedOptions<ConsoleLoggerOptions, ILoggerProviderConfiguration<ConsoleLoggerProvider>>(
        name: Options.DefaultName,
        dependency: sp.GetRequiredService<ILoggerProviderConfiguration<ConsoleLoggerProvider>>(),
        action: static (options, provider) => provider.Configuration.Bind(options)));

builder.Host.ConfigureMetrics(mb => mb.AddConfiguration(builder.Configuration.GetSection("Metrics")));

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

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

builder.Services.AddHealthChecks().AddMemoryCheck();

builder.Services.AddMqttServer();
//builder.Services.AddMqttAuthentication((userName, passwd) => true);
builder.Host.ConfigureMqttServer((ctx, builder) => builder.UseHttpServerWebSocketConnections());

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
    var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ??
        throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

    var dataDir = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
    if (!string.IsNullOrEmpty(dataDir))
    {
        Directory.CreateDirectory(dataDir);
    }

    builder.Services.AddMqttServerIdentity()
        .AddMqttServerIdentityStore(options => options
            .UseModel(ApplicationDbContextModel.Instance)
            .UseSqlite(connectionString));
    builder.Services.AddMqttServerUI();
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
app.UseAntiforgery();
app.UseAuthorization();

app.MapMqttWebSockets();

var group = app.MapGroup("/healthz");
group.MapHealthChecks("", new() { Predicate = check => check.Tags.Count == 0 });
group.MapMemoryHealthCheck("memory");

if (RuntimeOptions.WebUISupported)
{
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
        """).ConfigureAwait(false);
        await ctx.Response.CompleteAsync().ConfigureAwait(false);
    });
}

await CertificateGenerateInitializer.InitializeAsync(builder.Environment, builder.Configuration, CancellationToken.None).ConfigureAwait(false);

if (RuntimeOptions.WebUISupported)
{
    await InitializeIdentityExtensions.InitializeIdentityStoreAsync(app.Services).ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);