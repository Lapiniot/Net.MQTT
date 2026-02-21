using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Mqtt.Server.Identity;
using Mqtt.Server.Identity.Data;
using Mqtt.Server.Identity.Stores;

var builder = Host.CreateApplicationBuilder(args);

var identity = builder.Services
    .Configure<IdentityOptions>(options =>
    {
#if NET10_0_OR_GREATER
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
#else
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
#endif
    })
    .AddDbContext<ApplicationDbContext>(options =>
        options.ConfigureProvider(builder.Configuration));

var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);

// Sqlite EFCore provider will create database file if it doesn't exist. But it will not ensure that desired
// file location directory exists, so we must create data directory by ourselves.
if (builder.Configuration["DbProvider"] is "Sqlite" or "SQLite" or "" or null &&
    builder.Configuration.GetConnectionString("SqliteAppDbContextConnection") is { } connectionString)
{
    if (Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource) is { Length: > 0 } directory)
    {
        Directory.CreateDirectory(directory);
    }
}

await InitializeIdentityExtensions.InitializeIdentityStoreAsync(host.Services).ConfigureAwait(false);

await host.StopAsync().ConfigureAwait(false);