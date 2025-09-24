using System.Net.Sockets;

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
                .WithReference(source: postgresDb, connectionName: "NpgsqlAppDbContextConnection");
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
                .WithReference(source: sqlDb, connectionName: "SqlServerAppDbContextConnection");
            break;
        }
}

await builder.Build().RunAsync().ConfigureAwait(false);