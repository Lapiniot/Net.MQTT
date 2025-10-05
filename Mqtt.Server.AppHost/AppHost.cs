using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var mqttServer = builder.AddProject<Projects.Mqtt_Server>("mqtt-server")
    // Filter out Kestrel's Unix Domain Socket endpoints, because Aspire perharps doesn't support them
    .WithEndpointsInEnvironment(static ea => ea is { Port: not 0 })
    .WithEndpoint(name: "mqtt", port: 1883, scheme: "mqtt", protocol: ProtocolType.Tcp, isExternal: true, env: "MQTT__Endpoints__mqtt__Port")
    .WithEndpoint(name: "mqtts", port: 8883, scheme: "mqtts", protocol: ProtocolType.Tcp, isExternal: true, env: "MQTT__Endpoints__mqtts__Port")
    .WithEndpoint("mqtt", ea => ea.TargetHost = "*", createIfNotExists: false)
    .WithEndpoint("mqtts", ea => ea.TargetHost = "*", createIfNotExists: false)
    .WithUrls(static ctx =>
    {
        List<ResourceUrlAnnotation> extra = [];
        foreach (var annotation in ctx.Urls)
        {
            switch (annotation)
            {
                case { Endpoint.EndpointName: "mqtt", Url: var url }:
                    annotation.DisplayText = $"{url} (MQTT TCP)";
                    break;
                case { Endpoint.EndpointName: "mqtts", Url: var url }:
                    annotation.DisplayText = $"{url} (MQTT TCP.SSL)";
                    break;
                case { Endpoint.EndpointName: "mqtt-kestrel", Url: var url }:
                    url = new UriBuilder(url) { Scheme = "mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                    annotation.Url = url;
                    annotation.DisplayText = $"{url} (MQTT TCP over Kestrel)";
                    break;
                case { Endpoint.EndpointName: "mqtts-kestrel", Url: var url }:
                    url = new UriBuilder(url) { Scheme = "mqtts" }.Uri.AbsoluteUri.TrimEnd('/');
                    annotation.Url = url;
                    annotation.DisplayText = $"{url} (MQTT TCP.SSL over Kestrel)";
                    break;
                case { Endpoint: { EndpointName: "http" } endpoint, Url: var url }:
                    url = new UriBuilder(url) { Scheme = "ws", Path = "/mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                    extra.Add(new()
                    {
                        Endpoint = endpoint,
                        Url = url,
                        DisplayText = $"{url} (MQTT over WebSockets)"
                    });
                    break;
                case { Endpoint: { EndpointName: "https" } endpoint, Url: var url }:
                    url = new UriBuilder(url) { Scheme = "wss", Path = "/mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                    extra.Add(new()
                    {
                        Endpoint = endpoint,
                        Url = url,
                        DisplayText = $"{url} (MQTT over Secure WebSockets)"
                    });
                    break;
            }
        }

        foreach (var annotation in extra)
        {
            ctx.Urls.Add(annotation);
        }
    });

switch (builder.Configuration["DbProvider"])
{
    case "PostgreSQL" or "Npgsql":
        {
            var postgres = builder.AddPostgres("postgres")
                .WithDataVolume(name: "aspire-mqtt-server-postgres-data", isReadOnly: false)
                .WithPgAdmin();

            var postgresDb = postgres.AddDatabase("mqtt-server-db");

            mqttServer
                .WithEnvironment("MQTT_DbProvider", "PostgreSQL")
                .WithReference(source: postgresDb, connectionName: "NpgsqlAppDbContextConnection")
                .WaitFor(dependency: postgresDb);

            break;
        }

    case "MSSQL" or "SqlServer":
        {
            var sql = builder.AddSqlServer("mssql")
                .WithDataVolume("aspire-mqtt-server-mssql-data", false);

            var sqlDb = sql.AddDatabase("mqtt-server-db");

            mqttServer
                .WithEnvironment("MQTT_DbProvider", "MSSQL")
                .WithEnvironment("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false")
                .WithReference(source: sqlDb, connectionName: "SqlServerAppDbContextConnection")
                .WaitFor(dependency: sqlDb);

            break;
        }

    case "CosmosDB":
        {
            var cosmos = builder.AddAzureCosmosDB("cosmos-db");

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

            mqttServer
                .WithEnvironment("MQTT_DbProvider", "CosmosDB")
                .WithEnvironment("MQTT_ApplyMigrations", "true")
                .WithEnvironment("MQTT_CosmosDB__ConnectionMode", "Gateway")
                .WithReference(source: cosmosDb, connectionName: "CosmosAppDbContextConnection")
                .WaitFor(dependency: cosmosDb);

            break;
        }
}

await builder.Build().RunAsync().ConfigureAwait(false);