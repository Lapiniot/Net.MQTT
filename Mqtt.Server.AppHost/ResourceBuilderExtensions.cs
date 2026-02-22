using Microsoft.Extensions.Configuration;
using Projects;

namespace Mqtt.Server.AppHost;

internal static class ResourceBuilderExtensions
{
    extension<T>(IResourceBuilder<T> resourceBuilder)
        where T : Resource, IResourceWithEnvironment, IResourceWithWaitSupport
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
            if (resourceBuilder.ApplicationBuilder is { ExecutionContext.IsRunMode: true } builder)
            {
                resourceBuilder.WaitForCompletion(builder.AddDatabaseMigration("mqtt-server-migrate-db"));
            }

            return resourceBuilder.WithEnvironment("MQTT_DbProvider", "Sqlite");
        }

        public IResourceBuilder<T> WithPostgreSQLDatabase()
        {
            var postgres = resourceBuilder.ApplicationBuilder.AddPostgres("postgres")
                .WithDataVolume(name: "aspire-mqtt-server-postgres-data", isReadOnly: false);

            if (resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                postgres.WithPgAdmin();
            }

            var postgresDb = postgres.AddDatabase("mqtt-server-db");

            return resourceBuilder.WithDatabase(postgresDb, "PostgreSQL", "NpgsqlAppDbContextConnection");
        }

        public IResourceBuilder<T> WithSqlServerDatabase()
        {
            var sql = resourceBuilder.ApplicationBuilder.AddSqlServer("mssql")
                .WithDataVolume("aspire-mqtt-server-mssql-data", false);

            var sqlDb = sql.AddDatabase("mqtt-server-db");

            return resourceBuilder.WithDatabase(sqlDb, "MSSQL", "SqlServerAppDbContextConnection");
        }

        public IResourceBuilder<T> WithCosmosDatabase()
        {
            var builder = resourceBuilder.ApplicationBuilder;

            var cosmos = resourceBuilder.ApplicationBuilder.AddAzureCosmosDB("cosmos-db");

            if (builder.ExecutionContext.IsRunMode && builder.Configuration.GetValue<bool?>("CosmosDB:UseEmulator") is true)
            {
#pragma warning disable ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                cosmos.RunAsPreviewEmulator(emulator => emulator
                    .WithDataVolume()
                    .WithDataExplorer()
                    .WithGatewayPort(8081));
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
                .WithDatabase(cosmosDb, "CosmosDB", "CosmosAppDbContextConnection",
                    migrateResourceBuilderFactory: builder => builder.ExecutionContext.IsRunMode
                        ? builder
                            .AddDatabaseMigration($"{cosmosDb.Resource}-migrate")
                            .WithEnvironment("CosmosDB__ConnectionMode", "Gateway")
                        : null)
                .WithEnvironment("MQTT_CosmosDB__ConnectionMode", "Gateway");
        }

        public IResourceBuilder<T> WithDatabase(
            IResourceBuilder<IResourceWithConnectionString> database,
            string providerName, string connectionStringName)
        {
            return resourceBuilder.WithDatabase(database, providerName, connectionStringName,
                builder => builder.ExecutionContext.IsRunMode
                    ? builder.AddDatabaseMigration($"{database.Resource}-migrate")
                    : null);
        }

        public IResourceBuilder<T> WithDatabase<TMigrateResource>(
            IResourceBuilder<IResourceWithConnectionString> database,
            string providerName, string connectionStringName,
            Func<IDistributedApplicationBuilder, IResourceBuilder<TMigrateResource>?> migrateResourceBuilderFactory)
            where TMigrateResource : IResourceWithEnvironment, IResourceWithWaitSupport, IResource
        {
            if (migrateResourceBuilderFactory(resourceBuilder.ApplicationBuilder) is { } migrateBuilder)
            {
                migrateBuilder
                    .WithEnvironment("DbProvider", providerName)
                    .WithReference(database, connectionStringName)
                    .WaitFor(database);

                resourceBuilder.WaitForCompletion((IResourceBuilder<IResource>)migrateBuilder);
            }

            return resourceBuilder
                .WithEnvironment("MQTT_DbProvider", providerName)
                .WithReference(database, connectionStringName)
                .WaitFor(database);
        }

        public IResourceBuilder<T> WithPapercutSmtp()
        {
            if (resourceBuilder.ApplicationBuilder is { ExecutionContext.IsRunMode: true } builder)
            {
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
            }

            return resourceBuilder;
        }
    }

    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ProjectResource> AddDatabaseMigration(string name)
        {
            return builder
                .AddProject<Mqtt_Server_Migrate>(name, options =>
                {
                    options.ExcludeKestrelEndpoints = true;
                    options.ExcludeLaunchProfile = true;
                })
                .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
                .WithTargetFramework();
        }
    }

    extension(IResourceBuilder<ProjectResource> resourceBuilder)
    {
        /// <summary>
        /// Adds --framework argument to be passed to the project resource when it is launched or built for publishing.
        /// </summary>
        /// <remarks>
        /// TFM of the currently running Aspire host will be used 
        /// if <paramref name="framework"/> parameter is <see langword="null" />.
        /// </remarks>
        /// <param name="framework">Target Framework Moniker to be used when project resource is built.</param>
        /// <returns><see cref="IResourceBuilder{T}"/></returns>
        public IResourceBuilder<ProjectResource> WithTargetFramework(string? framework = null) =>
            resourceBuilder.WithArgs("--framework", framework ?? $"net{Environment.Version.ToString(2)}");
    }
}