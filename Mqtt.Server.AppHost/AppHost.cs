using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Mqtt.Server.AppHost;
using Net.Mqtt.Server.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var apiKeyParam = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder,
        name: "otlp-api-key", upper: false, special: false, minLower: 16, minNumeric: 16);

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(dasboard => dasboard
        .WithHostPort(8080)
        .WithForwardedHeaders(enabled: true)
        .WithEnvironment("DASHBOARD__OTLP__AUTHMODE", "ApiKey")
        .WithEnvironment("DASHBOARD__OTLP__PRIMARYAPIKEY", apiKeyParam));

if (builder.Configuration.GetValue<bool?>("RunAsContainer") is true)
{
    var server = builder.AddMqttServer("mqtt-server")
        .WithKestrelTcpEndpoint()
        .WithEnvironment("Logging__LogLevel__Default", "Information")
        .WithOtlpExporter(OtlpProtocol.Grpc)
        .WithApplicationDatabase()
        .WithPapercutSmtp();

    var publishWithSsl = builder.Configuration.GetValue<bool?>("WithSslConfiguration") ?? true;
    var publishWithDataVolume = builder.Configuration.GetValue<bool?>("WithPersistedDataVolume") ?? false;

    if (builder.ExecutionContext.IsRunMode || publishWithSsl)
    {
        server
            .WithTcpSslEndpoint()
            .WithKestrelTcpSslEndpoint();
    }

    if (builder.ExecutionContext.IsRunMode || publishWithDataVolume)
    {
        // Configure data volume to persist application data between runs
        // This makes sense only in run mode, because there could be other 
        // data storage options (bind mounts e.g.) considered for production deployments.
        // WithPersistedDataVolume:true configuration option will force default data volume
        // being added even in publish mode
        server.WithDataVolume();
    }

    if (builder.ExecutionContext.IsPublishMode)
    {
        server.WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", $"x-otlp-api-key={apiKeyParam}");

        if (publishWithSsl)
        {
            server.WithHttpsEndpointDefaults();

            var certPathParam = builder.AddParameter(
                name: "ssl-certificate-path",
                value: "/home/app/.ssl/mqtt-server.pfx",
                publishValueAsDefault: true,
                secret: false)
                .WithDescription("Path (inside container) to the SSL certificate file to be used for SSL encryption.");

            var certKeyPasswordParam = builder.AddParameter(
                name: "ssl-certificate-password",
                value: "",
                secret: true)
                .WithDescription("Password for the SSL certificate key file.");

            server
                .WithEnvironment("Kestrel__Certificates__Default__Path", certPathParam)
                .WithEnvironment("Kestrel__Certificates__Default__Password", certKeyPasswordParam)
                .WithEnvironment("MQTT__Certificates__Default__Path", certPathParam)
                .WithEnvironment("MQTT__Certificates__Default__Password", certKeyPasswordParam);

            server.WithBindMount("certificates", "/home/app/.ssl", isReadOnly: true);
        }
    }
}
else
{
    builder.AddProject<Projects.Mqtt_Server>("mqtt-server")
        .RunWithTargetFramework()
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
                        annotation.DisplayText = $"{url} (TCP)";
                        break;
                    case { Endpoint.EndpointName: "mqtts", Url: var url }:
                        annotation.DisplayText = $"{url} (TCP.SSL)";
                        break;
                    case { Endpoint.EndpointName: "mqtt-kestrel", Url: var url }:
                        url = new UriBuilder(url) { Scheme = "mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                        annotation.Url = url;
                        annotation.DisplayText = $"{url} (TCP via Kestrel)";
                        break;
                    case { Endpoint.EndpointName: "mqtts-kestrel", Url: var url }:
                        url = new UriBuilder(url) { Scheme = "mqtts" }.Uri.AbsoluteUri.TrimEnd('/');
                        annotation.Url = url;
                        annotation.DisplayText = $"{url} (TCP.SSL via Kestrel)";
                        break;
                    case { Endpoint: { EndpointName: "http" } endpoint, Url: var url }:
                        url = new UriBuilder(url) { Scheme = "ws", Path = "/mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                        extra.Add(new()
                        {
                            Endpoint = endpoint,
                            Url = url,
                            DisplayText = $"{url} (WebSockets)"
                        });
                        break;
                    case { Endpoint: { EndpointName: "https" } endpoint, Url: var url }:
                        url = new UriBuilder(url) { Scheme = "wss", Path = "/mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                        extra.Add(new()
                        {
                            Endpoint = endpoint,
                            Url = url,
                            DisplayText = $"{url} (Secure WebSockets)"
                        });
                        break;
                }
            }

            foreach (var annotation in extra)
            {
                ctx.Urls.Add(annotation);
            }
        })
        .WithExternalHttpEndpoints()
        .WithApplicationDatabase()
        .WithPapercutSmtp();
}

await builder.Build().RunAsync().ConfigureAwait(false);