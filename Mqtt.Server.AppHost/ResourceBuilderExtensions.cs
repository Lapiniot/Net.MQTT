using Microsoft.Extensions.Configuration;

namespace Mqtt.Server.AppHost;

internal static class ResourceBuilderExtensions
{
    extension<T>(IResourceBuilder<T> resourceBuilder) where T : Resource, IResourceWithEnvironment, IResourceWithWaitSupport
    {
        public IResourceBuilder<T> WithApplicationDatabase()
        {
            return resourceBuilder.ApplicationBuilder.Configuration["DbProvider"] switch
            {
                "Sqlite" or "SQLite" or "" or null => resourceBuilder.WithSqliteDatabase(),
                "PostgreSQL" or "Npgsql" => resourceBuilder.WithPostgreSQLDatabase(),
                "MSSQL" or "SqlServer" => resourceBuilder.WithSqlServerDatabase(),
                "CosmosDB" => resourceBuilder.WithCosmosDatabase(),
                _ => throw new InvalidOperationException("Unsupported database provider. Please specify one of the" +
                    " following values in configuration: Sqlite, PostgreSQL, MSSQL, CosmosDB.")
            };
        }

        public IResourceBuilder<T> WithSqliteDatabase()
        {
            return resourceBuilder
                .WithEnvironment("MQTT_DbProvider", "Sqlite")
                .WithEnvironment("MQTT_ApplyMigrations", "true");
        }

        public IResourceBuilder<T> WithPostgreSQLDatabase()
        {
            var postgres = resourceBuilder.ApplicationBuilder.AddPostgres("postgres")
                .WithDataVolume(name: "aspire-mqtt-server-postgres-data", isReadOnly: false)
                .WithPgAdmin();

            var postgresDb = postgres.AddDatabase("mqtt-server-db");

            return resourceBuilder
                .WithEnvironment("MQTT_DbProvider", "PostgreSQL")
                .WithEnvironment("MQTT_ApplyMigrations", "true")
                .WithReference(source: postgresDb, connectionName: "NpgsqlAppDbContextConnection")
                .WaitFor(dependency: postgresDb);
        }

        public IResourceBuilder<T> WithSqlServerDatabase()
        {
            var sql = resourceBuilder.ApplicationBuilder.AddSqlServer("mssql")
                .WithDataVolume("aspire-mqtt-server-mssql-data", false);

            var sqlDb = sql.AddDatabase("mqtt-server-db");

            return resourceBuilder
                .WithEnvironment("MQTT_DbProvider", "MSSQL")
                .WithEnvironment("MQTT_ApplyMigrations", "true")
                .WithEnvironment("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false")
                .WithReference(source: sqlDb, connectionName: "SqlServerAppDbContextConnection")
                .WaitFor(dependency: sqlDb);
        }

        public IResourceBuilder<T> WithCosmosDatabase()
        {
            var builder = resourceBuilder.ApplicationBuilder;

            var cosmos = resourceBuilder.ApplicationBuilder.AddAzureCosmosDB("cosmos-db");

            if (builder.Configuration.GetValue<bool?>("CosmosDB:UseEmulator") is true)
            {
#pragma warning disable ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                cosmos.RunAsPreviewEmulator(emulator => emulator
                    .WithDataVolume()
                    .WithDataExplorer()
                    .WithGatewayPort(8081)
                    .WithLifetime(ContainerLifetime.Persistent));
#pragma warning restore ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
            else
            {
                var accountName = builder.AddParameterFromConfiguration("CosmosAccountName",
                    configurationKey: "CosmosDB:AccountName")
                    .WithDescription("The name of existing Azure Cosmos DB account that you want to connect to.");
                var resourceGroupName = builder.AddParameterFromConfiguration("CosmosResourceGroup",
                    configurationKey: "CosmosDB:ResourceGroup")
                    .WithDescription("The name of existing resource group (leave empty to use current resource group).");

                cosmos.RunAsExisting(accountName, resourceGroupName)
                    .WithAccessKeyAuthentication();
            }

            var cosmosDb = cosmos.AddCosmosDatabase("mqtt-server-db");

            return resourceBuilder
                .WithEnvironment("MQTT_DbProvider", "CosmosDB")
                .WithEnvironment("MQTT_ApplyMigrations", "true")
                .WithEnvironment("MQTT_CosmosDB__ConnectionMode", "Gateway")
                .WithReference(source: cosmosDb, connectionName: "CosmosAppDbContextConnection")
                .WaitFor(dependency: cosmosDb);
        }

        public IResourceBuilder<T> WithPapercutSmtp()
        {
            var builder = resourceBuilder.ApplicationBuilder;
            var papercut = builder.AddPapercutSmtp("papercut");

            resourceBuilder
                .WithReference(papercut, "SmtpServer")
                .WithEnvironment(ctx =>
                {
                    var endpointReference = papercut.GetEndpoint("smtp");
                    ctx.EnvironmentVariables["MQTT_SMTP__Host"] = endpointReference.Host;
                    ctx.EnvironmentVariables["MQTT_SMTP__Port"] = endpointReference.Port;
                })
                .WaitFor(papercut);

            return resourceBuilder;
        }
    }
}