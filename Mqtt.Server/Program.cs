using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.Metrics;
using Mqtt.Server.Identity;
using Mqtt.Server.Identity.Data.Compiled;
using Mqtt.Server.Web;
using OOs.Extensions.Configuration;
using OOs.Reflection;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

Console.WriteLine();
Console.WriteLine(Assembly.GetEntryAssembly().BuildLogoString());
Console.WriteLine();

var builder = WebApplication.CreateSlimBuilder(args);

#region Host configuration

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddPlatformSpecificJsonFile(true, true);
}

builder.Configuration.AddEnvironmentVariables("MQTT_");

var useAdminWebUI = builder.Configuration.TryGetSwitch("UseAdminWebUI", out var enabled) && enabled;

if (builder.Configuration.TryGetSwitch("MetricsCollectionSupport", out enabled))
{
    AppContext.SetSwitch("Net.Mqtt.Server.MetricsCollectionSupport", enabled);
}

#endregion

builder.Host.ConfigureMetrics(mb => mb.AddConfiguration(builder.Configuration.GetSection("Metrics")));

builder.WebHost.UseKestrelHttpsConfiguration();
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

builder.Services.AddHealthChecks().AddMemoryCheck();

builder.Services.AddMqttServer();
//builder.Services.AddMqttAuthentication((userName, passwd) => true);
builder.Host.ConfigureMqttServer((ctx, builder) => builder.InterceptWebSocketConnections());

#region Authorization / Authentication

builder.Services.AddSingleton<IEmailSender, NoOpEmailSender>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;
    })
    .AddCertificateCache()
    .AddJwtBearer()
    .AddIdentityCookies();

#endregion

if (useAdminWebUI)
{
    var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ??
        throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

    builder.Services.AddMqttServerIdentity()
        .AddMqttServerIdentityStore(options => options
            .UseModel(ApplicationDbContextModel.Instance)
            .UseSqlite(connectionString));
    builder.Services.AddMqttServerUI();
}

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

if (useAdminWebUI)
{
    app.UseAuthorization();
    app.UseAntiforgery();
    app.MapMqttServerUI();
}

app.UseWebSockets();
app.MapWebSocketInterceptor("/mqtt");

app.MapHealthChecks("/health", new() { Predicate = check => check.Tags.Count == 0 });
app.MapMemoryHealthCheck("/health/memory");

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "data"));

if (useAdminWebUI)
{
    await app.Services.InitializeMqttServerIdentityStoreAsync().ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);