using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Mqtt.Server.Identity.CosmosDB;
using Mqtt.Server.Identity.PostgreSQL;
using Mqtt.Server.Identity.Sqlite;
using Mqtt.Server.Identity.SqlServer;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1708 // Identifiers should differ by more than case

namespace Mqtt.Server.Identity.Stores;

public static class MqttServerIdentityExtensions
{
    extension(IdentityBuilder builder)
    {
        public IdentityBuilder AddMqttServerIdentityStores(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (RuntimeOptions.CosmosDBSupported && configuration["DbProvider"] is "CosmosDB")
            {
                builder.AddCosmosIdentityStores();
            }

            return builder.AddMqttServerIdentityStores(options => options.ConfigureProvider(configuration));
        }
    }

    extension(DbContextOptionsBuilder options)
    {
        public void ConfigureProvider(IConfiguration configuration, string? connectionString = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(configuration);

            switch (configuration.GetValue<DbProvider?>("DbProvider"))
            {
                case DbProvider.SQLite or null:
                    options.ConfigureSqlite(connectionString ?? GetConnectionString("SqliteAppDbContextConnection"));
                    break;
                case DbProvider.PostgreSQL or DbProvider.Npgsql:
                    if (RuntimeOptions.PostgreSQLSupported)
                    {
                        options.ConfigureNpgsql(connectionString ?? GetConnectionString("NpgsqlAppDbContextConnection"));
                    }
                    else
                    {
                        throw new NotSupportedException("PostgreSQL support is not enabled in this runtime configuration.");
                    }

                    break;
                case DbProvider.MSSQL or DbProvider.SqlServer:
                    if (RuntimeOptions.MSSQLSupported)
                    {
                        options.ConfigureSqlServer(connectionString ?? GetConnectionString("SqlServerAppDbContextConnection"));
                    }
                    else
                    {
                        throw new NotSupportedException("MSSQL support is not enabled in this runtime configuration.");
                    }

                    break;
                case DbProvider.CosmosDB:
                    if (RuntimeOptions.CosmosDBSupported)
                    {
                        options.ConfigureCosmos(connectionString ?? GetConnectionString("CosmosAppDbContextConnection"),
                            databaseName: "mqtt-server-db",
                            cosmosOptions => cosmosOptions.Configure(configuration.GetSection("CosmosDB")));
                    }
                    else
                    {
                        throw new NotSupportedException("Azure CosmosDB support is not enabled in this runtime configuration.");
                    }

                    break;
                case { } unsupported:
                    throw new NotSupportedException($"Unsupported provider: '{unsupported}'.");
            }

            string GetConnectionString(string name) =>
                configuration.GetConnectionString(name)
                    ?? throw new InvalidOperationException($"Connection string '{name}' not found.");
        }
    }
}