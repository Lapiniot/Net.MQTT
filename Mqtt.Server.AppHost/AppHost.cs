using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Microsoft.Extensions.Configuration;
using Mqtt.Server.AppHost;
using Net.Mqtt.Server.Aspire.Hosting;

#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var builder = DistributedApplication.CreateBuilder(args);

var apiKeyParam = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder,
    name: "otlp-api-key", upper: false, special: false, minLower: 16, minNumeric: 16);

builder
    .AddDockerComposeEnvironment("compose")
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
        .RunWithSecureEndpoints(AddSecureEndpoints)
        .WithEnvironment("Logging__LogLevel__Default", "Information")
        .WithApplicationDatabase(ConfigureMigrator)
        .WithPapercutSmtp()
        .WithOtlpExporter(OtlpProtocol.Grpc)
        .PublishWithOtlpApiKey(apiKeyParam)
        .PublishAsDockerComposeService(ConfigureDockerCompose);

    if (publishWithSsl)
    {
        server.PublishWithSecureEndpoints(AddSecureEndpoints);
    }
}
else
{
    var server = builder.AddProject<Projects.Mqtt_Server>("mqtt-server", options =>
        {
            options.ExcludeKestrelEndpoints = true;
            options.ExcludeLaunchProfile = true;
        })
        .RunWithTargetFramework()
        .WithHttpEndpointDefaults()
        .WithTcpEndpoint()
        .WithKestrelTcpEndpoint()
        .RunWithSecureEndpoints(AddSecureEndpoints)
        .WithExternalHttpEndpoints()
        .WithEnvironment("Logging__LogLevel__Default", "Information")
        .WithApplicationDatabase(ConfigureMigrator)
        .WithPapercutSmtp()
        .PublishAsDockerComposeService(ConfigureDockerCompose);

    if (publishWithSsl)
    {
        server.PublishWithSecureEndpoints(AddSecureEndpoints);
    }
}

void AddSecureEndpoints<T>(IResourceBuilder<T> builder)
    where T : IResourceWithEndpoints, IResourceWithEnvironment, IResourceWithArgs
{
    builder
        .WithHttpsEndpointDefaults()
        .WithTcpSslEndpoint()
        .WithKestrelTcpSslEndpoint();
}

void ConfigureMigrator(IResourceBuilder<IResourceWithEnvironment> migrator)
{
    migrator
        .WithEnvironment("Logging__LogLevel__Default", "Information")
        .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
        .WithOtlpExporter(OtlpProtocol.Grpc)
        .PublishWithOtlpApiKey(apiKeyParam);
}

void ConfigureDockerCompose(DockerComposeServiceResource resource, Service service)
{
    if (publishWithSsl)
    {
        var bindSourceParameter = builder.AddParameterFromConfiguration("ssl-certificates-bindmount-source",
            "SSL_CERTIFICATES_BINDMOUNT_SOURCE", secret: false);
        var certPathParam = builder.AddParameter("ssl-certificate-path", "/home/app/.ssl/mqtt-server.pfx", true, false);
        var certPasswordParam = builder.AddParameter("ssl-certificate-password", "", true);

        service.AddVolume(new Volume()
        {
            Name = "certificates",
            Source = bindSourceParameter.AsEnvironmentPlaceholder(resource),
            Target = "/home/app/.ssl",
            Type = "bind",
            ReadOnly = true
        });

        service
            .AddEnvironmentalVariable("Kestrel__Certificates__Default__Path", certPathParam.AsEnvironmentPlaceholder(resource))
            .AddEnvironmentalVariable("Kestrel__Certificates__Default__Password", certPasswordParam.AsEnvironmentPlaceholder(resource))
            .AddEnvironmentalVariable("MQTT__Certificates__Default__Path", certPathParam.AsEnvironmentPlaceholder(resource))
            .AddEnvironmentalVariable("MQTT__Certificates__Default__Password", certPasswordParam.AsEnvironmentPlaceholder(resource));
    }
}

await builder.Build().RunAsync().ConfigureAwait(false);