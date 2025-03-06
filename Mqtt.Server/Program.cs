using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.Metrics;
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

builder.Logging.AddSimpleConsole(b => b.SingleLine = true);

builder.Host.ConfigureMetrics(mb => mb.AddConfiguration(builder.Configuration.GetSection("Metrics")));

builder.WebHost
    .UseKestrelHttpsConfiguration()
    .UseMqttIntegration()
    .ConfigureKestrel(options => options.ListenAnyIP(1884, builder => builder.UseMqttServer()))
    .UseQuic(options =>
    {
        // Configure server defaults to match client defaults.
        options.DefaultStreamErrorCode = 0x10c; // H3_REQUEST_CANCELLED (0x10C)
        options.DefaultCloseErrorCode = 0x100; // H3_NO_ERROR (0x100)
    });

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

if (RuntimeOptions.WebUISupported)
{
    app.UseRouting();
    app.UseAntiforgery();
#if NET9_0_OR_GREATER
    app.MapStaticAssets();
#else
    app.UseStaticFiles();
#endif
    app.UseAuthorization();
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

app.MapMqttWebSockets();

var group = app.MapGroup("/health");
group.MapHealthChecks("", new() { Predicate = check => check.Tags.Count == 0 });
group.MapMemoryHealthCheck("memory");

await CertificateGenerateInitializer.InitializeAsync(builder.Environment, builder.Configuration, CancellationToken.None).ConfigureAwait(false);

if (RuntimeOptions.WebUISupported)
{
    await InitializeIdentityExtensions.InitializeIdentityStoreAsync(app.Services).ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);