using Aspire.Hosting.Docker;
using Microsoft.Extensions.Configuration;
using Projects;
using IRWE = Aspire.Hosting.ApplicationModel.IResourceWithEnvironment;
using IRWWS = Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport;

#pragma warning disable ASPIRECOMPUTE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Mqtt.Server.AppHost;

internal static class ResourceBuilderExtensions
{
    private const string DbProviderVarName = "MQTT_DbProvider";

    extension<T>(IResourceBuilder<T> resourceBuilder) where T : Resource, IRWE, IRWWS
    {
        public IResourceBuilder<T> WithApplicationDatabase(Action<IResourceBuilder<IRWE>>? configureMigrate = null)
        {
            return resourceBuilder.ApplicationBuilder.Configuration["DbProvider"] switch
            {
                "Sqlite" or "SQLite" or "" or null => resourceBuilder.WithSqliteDatabase(configureMigrate),
                "PostgreSQL" or "Npgsql" => resourceBuilder.WithPostgreSQLDatabase(configureMigrate),
                "MSSQL" or "SqlServer" => resourceBuilder.WithSqlServerDatabase(configureMigrate),
                "CosmosDB" => resourceBuilder.WithCosmosDatabase(configureMigrate),
                _ => throw new InvalidOperationException("Unsupported database provider. Please specify one of the" +
                    " following values in configuration: Sqlite, PostgreSQL, MSSQL, CosmosDB.")
            };
        }

        public IResourceBuilder<T> WithSqliteDatabase(Action<IResourceBuilder<IRWE>>? configureMigrate = null)
        {
            const string ProviderValue = "Sqlite";

            var builder = resourceBuilder.ApplicationBuilder;

            resourceBuilder.WithEnvironment(DbProviderVarName, ProviderValue);

            if (!builder.UseDatabaseMigrations())
            {
                return resourceBuilder;
            }

            IResourceBuilder<IRWE> migrate = resourceBuilder.Resource.IsContainer()
                ? builder.AddDatabaseMigrationContainer("mqtt-server-migrate-db")
                : builder.AddDatabaseMigration("mqtt-server-migrate-db");

            if (resourceBuilder.Resource.IsContainer()
                || builder.ExecutionContext.IsPublishMode
                    && resourceBuilder.Resource.RequiresImageBuild())
            {
                resourceBuilder.WithSqliteContainerConfiguration(migrate);
            }
            else
            {
                var connectionString = builder.AddConnectionString(name: "SqliteAppDbContextConnection");

                migrate
                    .WithEnvironment(DbProviderVarName, ProviderValue)
                    .WithReference(connectionString)
                    .WithParentRelationship(resourceBuilder);

                resourceBuilder
                    .WithReference(connectionString)
                    .WaitForCompletion(migrate);
            }

            configureMigrate?.Invoke(migrate);

            return resourceBuilder;
        }

        private IResourceBuilder<T> WithSqliteContainerConfiguration<TMigrate>(IResourceBuilder<TMigrate> migrate)
            where TMigrate : IRWE
        {
            const string ProviderValue = "Sqlite";

            var builder = resourceBuilder.ApplicationBuilder;
            var connectionString = builder.AddConnectionString(name: "SqliteAppDbContextConnection",
                ReferenceExpression.Create($"Data Source=/home/app/.local/share/mqtt-server/app.db;Cache=Shared"));

            // Add named volume mount to be shared between both mqtt-server and mqtt-server-migrate containers.
            var volumeMountAnnotation = new ContainerMountAnnotation(
                source: VolumeNameGenerator.Generate(resourceBuilder, "app-data"),
                target: "/home/app",
                type: ContainerMountType.Volume,
                isReadOnly: false);

            migrate
                .WithAnnotation(volumeMountAnnotation)
                .WithEnvironment(DbProviderVarName, ProviderValue)
                .WithReference(connectionString)
                .WithParentRelationship(resourceBuilder);

            return resourceBuilder
                .WithAnnotation(volumeMountAnnotation)
                .WithReference(connectionString)
                .WaitForCompletion((IResourceBuilder<IResource>)migrate);
        }

        public IResourceBuilder<T> WithPostgreSQLDatabase(Action<IResourceBuilder<IRWE>>? configureMigrate = null)
        {
            var postgres = resourceBuilder.ApplicationBuilder.AddPostgres("postgres")
                .WithDataVolume(name: "aspire-mqtt-server-postgres-data", isReadOnly: false);

            if (resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                postgres.WithPgAdmin();
            }

            var postgresDb = postgres.AddDatabase("mqtt-server-db");

            return resourceBuilder.WithDatabase(postgresDb, "PostgreSQL", "NpgsqlAppDbContextConnection", configureMigrate);
        }

        public IResourceBuilder<T> WithSqlServerDatabase(Action<IResourceBuilder<IRWE>>? configureMigrate = null)
        {
            var sql = resourceBuilder.ApplicationBuilder.AddSqlServer("mssql")
                .WithDataVolume("aspire-mqtt-server-mssql-data", false);

            var sqlDb = sql.AddDatabase("mqtt-server-db");

            return resourceBuilder.WithDatabase(sqlDb, "MSSQL", "SqlServerAppDbContextConnection", configureMigrate);
        }

        public IResourceBuilder<T> WithCosmosDatabase(Action<IResourceBuilder<IRWE>>? configureMigrate = null)
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

            void Configure(IResourceBuilder<IRWE> migrate)
            {
                migrate.WithEnvironment("MQTT_CosmosDB__ConnectionMode", "Gateway");
                configureMigrate?.Invoke(migrate);
            }

            return resourceBuilder
                .WithDatabase(cosmosDb, "CosmosDB", "CosmosAppDbContextConnection", Configure)
                .WithEnvironment("MQTT_CosmosDB__ConnectionMode", "Gateway");
        }

        public IResourceBuilder<T> WithDatabase(IResourceBuilder<IResourceWithConnectionString> database,
            string providerName, string connectionStringName,
            Action<IResourceBuilder<IRWE>>? configureMigrate = null)
        {
            IResourceBuilder<IRWE>? Factory(IDistributedApplicationBuilder builder)
            {
                var name = $"{database.Resource.Name}-migrate";
                return builder.UseDatabaseMigrations()
                    ? resourceBuilder.Resource.IsContainer()
                        ? builder.AddDatabaseMigrationContainer(name)
                        : builder.AddDatabaseMigration(name)
                    : null;
            }

            return resourceBuilder.WithDatabase(database, providerName, connectionStringName, Factory, configureMigrate);
        }

        public IResourceBuilder<T> WithDatabase(IResourceBuilder<IResourceWithConnectionString> database,
            string providerName, string connectionStringName,
            Func<IDistributedApplicationBuilder, IResourceBuilder<IRWE>?> migrateResourceBuilderFactory,
            Action<IResourceBuilder<IRWE>>? configureMigrate = null)
        {
            if (migrateResourceBuilderFactory(resourceBuilder.ApplicationBuilder) is { } migrateBuilder)
            {
                migrateBuilder
                    .WithEnvironment(DbProviderVarName, providerName)
                    .WithReference(database, connectionStringName)
                    .WithParentRelationship(resourceBuilder);

                configureMigrate?.Invoke(migrateBuilder);

                if (migrateBuilder is IResourceBuilder<IRWWS> withWaitSupport)
                {
                    withWaitSupport.WaitFor(database);
                }

                resourceBuilder.WaitForCompletion(migrateBuilder);
            }

            return resourceBuilder
                .WithEnvironment(DbProviderVarName, providerName)
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
        public bool UseDatabaseMigrations() => builder.Configuration.GetValue<bool?>("WithDbMigrations") ?? true;

        public IResourceBuilder<ProjectResource> AddDatabaseMigration(string name)
        {
            return builder.AddProject<Mqtt_Server_Migrate>(name, options =>
                {
                    options.ExcludeKestrelEndpoints = true;
                    options.ExcludeLaunchProfile = true;
                })
                .RunWithTargetFramework();
        }

        public IResourceBuilder<ContainerResource> AddDatabaseMigrationContainer(string name)
        {
            return builder.AddContainer(name, image: "lapiniot/mqtt-server-migrate", tag: "latest")
                .WithImageRegistry("docker.io");
        }
    }

    extension(IResourceBuilder<ProjectResource> resourceBuilder)
    {
        /// <summary>
        /// Adds --framework argument to be passed to the project resource when it is launched.
        /// </summary>
        /// <remarks>
        /// TFM of the currently running Aspire host will be used 
        /// if <paramref name="framework"/> parameter is <see langword="null" />.
        /// </remarks>
        /// <param name="framework">Target Framework Moniker to be used when project resource is built.</param>
        /// <returns><see cref="IResourceBuilder{T}"/></returns>
        public IResourceBuilder<ProjectResource> RunWithTargetFramework(string? framework = null) =>
            resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode
                ? resourceBuilder.WithArgs("--framework", framework ?? $"net{Environment.Version.ToString(2)}")
                : resourceBuilder;
    }

    extension(IResourceBuilder<DockerComposeAspireDashboardResource> resourceBuilder)
    {
        public IResourceBuilder<DockerComposeAspireDashboardResource> WithOtlpEndpointApiKeyAuth<TValue>(TValue apiKey)
            where TValue : IValueProvider, IManifestExpressionProvider
        {
            return resourceBuilder
                .WithEnvironment("DASHBOARD__OTLP__AUTHMODE", "ApiKey")
                .WithEnvironment("DASHBOARD__OTLP__PRIMARYAPIKEY", apiKey);
        }
    }

    extension<T>(IResourceBuilder<T> resourceBuilder) where T : IRWE
    {
        public IResourceBuilder<T> WithOtlpApiKey<TValue>(TValue apiKey)
            where TValue : IValueProvider, IManifestExpressionProvider =>
            resourceBuilder.WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", $"x-otlp-api-key={apiKey}");

        public IResourceBuilder<T> RunWithOtlpApiKey<TValue>(TValue apiKey)
            where TValue : IValueProvider, IManifestExpressionProvider
        {
            return resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode
                ? resourceBuilder.WithOtlpApiKey(apiKey)
                : resourceBuilder;
        }

        public IResourceBuilder<T> PublishWithOtlpApiKey<TValue>(TValue apiKey)
            where TValue : IValueProvider, IManifestExpressionProvider
        {
            return resourceBuilder.ApplicationBuilder.ExecutionContext.IsPublishMode
                ? resourceBuilder.WithOtlpApiKey(apiKey)
                : resourceBuilder;
        }
    }
}