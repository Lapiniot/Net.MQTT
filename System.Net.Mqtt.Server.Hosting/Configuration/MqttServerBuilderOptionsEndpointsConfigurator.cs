using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class MqttServerBuilderOptionsEndpointsConfigurator : IConfigureOptions<MqttServerBuilderOptions>
{
    internal const string RootSectionName = "MQTT";
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;

    public MqttServerBuilderOptionsEndpointsConfigurator(IConfiguration configuration, IHostEnvironment environment)
    {
        this.configuration = configuration;
        this.environment = environment;
    }

    private static SslProtocols ResolveSslProtocolsOptions(IConfigurationSection configuration)
    {
        var protocols = SslProtocols.None;

        if (!configuration.Exists()) return protocols;

        if (configuration.Value is not null)
        {
            protocols = Enum.Parse<SslProtocols>(configuration.Value);
        }
        else
        {
            foreach (var p in configuration.GetChildren())
            {
                protocols |= Enum.Parse<SslProtocols>(p.Value);
            }
        }

        return protocols;
    }

    private static CertificateOptions ResolveCertificateOptions(IConfigurationSection certificate, IConfiguration certificates)
    {
        var certName = certificate.Value;

        if (certName is null) return certificate.Get<CertificateOptions>();

        var certSection = certificates.GetSection(certName);

        return certSection.Exists()
            ? certSection.Get<CertificateOptions>()
            : throw new InvalidOperationException($"Certificate configuration for '{certName}' is missing");
    }

    public void Configure(MqttServerBuilderOptions options)
    {
        var section = configuration.GetSection(RootSectionName);
        var endpoints = section.GetSection("Endpoints");
        var certificates = section.GetSection("Certificates");

        foreach (var config in endpoints.GetChildren())
        {
            var certificateSection = config.GetSection("Certificate");

            if (certificateSection.Exists())
            {
                var protocols = ResolveSslProtocolsOptions(config.GetSection("SslProtocols"));

                var certOptions = ResolveCertificateOptions(certificateSection, certificates);

                if (certOptions.Path is not null)
                {
                    var path = environment.ContentRootFileProvider.GetFileInfo(certOptions.Path).PhysicalPath;
                    var keyPath = environment.ContentRootFileProvider.GetFileInfo(certOptions.KeyPath).PhysicalPath;
                    var password = certOptions.Password;

                    options.UseSslEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")),
                        () => CertificateLoader.LoadFromFile(path, keyPath, password), protocols,
                        config.GetValue("ClientCertificateMode", ClientCertificateMode.NoCertificate));
                }
                else if (certOptions.Subject is not null)
                {
                    var store = certOptions.Store;
                    var location = certOptions.Location;
                    var subject = certOptions.Subject;
                    var allowInvalid = certOptions.AllowInvalid;

                    options.UseSslEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")),
                        () => CertificateLoader.LoadFromStore(store, location, subject, allowInvalid), protocols,
                        config.GetValue("ClientCertificateMode", ClientCertificateMode.NoCertificate));
                }
                else
                {
                    throw new InvalidOperationException("Cannot load certificate from configuration, either store information or file path should be provided");
                }
            }
            else
            {
                options.UseEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")));
            }
        }
    }
}