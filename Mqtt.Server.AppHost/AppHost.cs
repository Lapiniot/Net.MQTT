using Microsoft.Extensions.Configuration;
using Mqtt.Server.AppHost;
using Net.Mqtt.Server.Aspire.Hosting;

#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var builder = DistributedApplication.CreateBuilder(args);

var apiKeyParam = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder,
    name: "otlp-api-key", upper: false, special: false, minLower: 16, minNumeric: 16);

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(dasboard => dasboard
        .WithHostPort(8080)
        .WithForwardedHeaders(enabled: true)
        .WithOtlpApiKey(apiKeyParam));

var runAsContainer = builder.Configuration.GetValue<bool?>("RunAsContainer") ?? false;
var publishWithSsl = builder.Configuration.GetValue<bool?>("WithSslConfiguration") ?? true;

if (runAsContainer)
{
    var server = builder.AddMqttServer("mqtt-server")
        .WithKestrelTcpEndpoint()
        .RunWithSecureEndpoints(builder => builder
            .WithHttpsEndpointDefaults()
            .WithTcpSslEndpoint()
            .WithKestrelTcpSslEndpoint())
        .WithEnvironment("Logging__LogLevel__Default", "Information")
        .WithApplicationDatabase(migrator => migrator
            .WithEnvironment("Logging__LogLevel__Default", "Information")
            .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithOtlpExporter(OtlpProtocol.Grpc)
            .PublishWithOtlpApiKey(apiKeyParam))
        .WithPapercutSmtp()
        .WithOtlpExporter(OtlpProtocol.Grpc)
        .PublishWithOtlpApiKey(apiKeyParam);

    if (builder.ExecutionContext.IsPublishMode && publishWithSsl)
    {
        server
            .WithHttpsEndpointDefaults()
            .WithTcpSslEndpoint()
            .WithKestrelTcpSslEndpoint();

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
else
{
    var server = builder.AddProject<Projects.Mqtt_Server>("mqtt-server",
            options => options.ExcludeKestrelEndpoints = true)
        .RunWithTargetFramework()
        .WithHttpEndpointDefaults()
        .WithTcpEndpoint()
        .WithKestrelTcpEndpoint()
        .RunWithSecureEndpoints(builder => builder
            .WithHttpsEndpointDefaults()
            .WithTcpSslEndpoint()
            .WithKestrelTcpSslEndpoint())
        .WithExternalHttpEndpoints()
        .WithApplicationDatabase()
        .WithPapercutSmtp();
}

await builder.Build().RunAsync().ConfigureAwait(false);