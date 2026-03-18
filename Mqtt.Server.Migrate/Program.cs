using System.Collections.Immutable;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mqtt.Server.Identity;
using Mqtt.Server.Identity.Data;
using Mqtt.Server.Identity.Stores;
using Mqtt.Server.Migrate;
using OOs.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Metrics;

#pragma warning disable CA1031 // Do not catch general exception types

[assembly: GenerateProductInfo]

(IReadOnlyDictionary<string, string>, ImmutableArray<string>) result = default;

try
{
    result = Arguments.Parse(args);
}
catch (Exception e)
{
    Console.WriteLine($"\e[91m{e.Message}\e[39m");
    return 1;
}

var (options, arguments) = result;

if (options.TryGetValue("PrintVersion", out var value) && bool.TryParse(value, out var printVersion) && printVersion)
{
    Console.WriteLine();
    Console.WriteLine($"{ProductInfo.Description} v{ProductInfo.InformationalVersion} ({ProductInfo.Copyright})");
    Console.WriteLine();
    return 0;
}
else if (options.TryGetValue("PrintHelp", out value) && bool.TryParse(value, out var printHelp) && printHelp)
{
    Console.WriteLine();
    Console.WriteLine($"Usage: {Path.GetFileName(Environment.ProcessPath)} [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine(Arguments.GetSynopsis());
    Console.WriteLine();
    return 0;
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings()
{
    Args = [.. arguments], // Redirect unknown options as arguments to the built-in CommandLineConfigurationProvider
    ContentRootPath = AppContext.BaseDirectory
});

builder.Configuration
    .AddEnvironmentVariables("MQTT_")
    .AddCommandArguments(options, arguments);

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

var telemetry = builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddRuntimeInstrumentation())
    .WithTracing(tracing => tracing.AddSource(builder.Environment.ApplicationName));

if (builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is { Length: > 0 })
{
    telemetry.UseOtlpExporter();
}

var identity = builder.Services
    .Configure<IdentityOptions>(options =>
    {
#if NET10_0_OR_GREATER
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
#else
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
#endif
    })
    .AddDbContext<ApplicationDbContext>(options => options.ConfigureProvider(
        configuration: builder.Configuration,
        connectionString: builder.Configuration.GetConnectionString("AppDbContextConnection")));

var host = builder.Build();

try
{
    await host.StartAsync().ConfigureAwait(false);

    // Sqlite EFCore provider will create database file if it doesn't exist. But it will not ensure that desired
    // file location directory exists, so we must create data directory by ourselves.
    if (builder.Configuration.GetValue<DbProvider?>("DbProvider") is DbProvider.SQLite or null &&
        (builder.Configuration.GetConnectionString("AppDbContextConnection") ??
        builder.Configuration.GetConnectionString("SqliteAppDbContextConnection")) is { } connectionString)
    {
        if (Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }
    }

    await InitializeIdentityExtensions.InitializeIdentityStoreAsync(host.Services).ConfigureAwait(false);

    await host.StopAsync().ConfigureAwait(false);
}
catch (Exception e)
{
    Console.WriteLine($"\e[91m{e.Message}\e[39m");
    return 1;
}
finally
{
    if (host is IAsyncDisposable asyncDisposable)
    {
        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
    }
    else
    {
        host.Dispose();
    }
}

Console.WriteLine("\e[92m\nDONE.\e[39m");
return 0;