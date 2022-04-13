using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class MqttServerBuilderOptionsEndpointsConfigurator : IConfigureOptions<MqttServerBuilderOptions>
{
    internal const string RootSectionName = "MQTT";
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ICertificateValidationPolicy validationPolicy;

    public MqttServerBuilderOptionsEndpointsConfigurator(IConfiguration configuration, IHostEnvironment environment,
        ICertificateValidationPolicy validationPolicy = null)
    {
        this.configuration = configuration;
        this.environment = environment;
        this.validationPolicy = validationPolicy;
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

                var certMode = config.GetValue<ClientCertificateMode?>("ClientCertificateMode", null);

                var policy = certMode switch
                {
                    ClientCertificateMode.NoCertificate => NoCertificatePolicy.Instance,
                    ClientCertificateMode.AllowCertificate => AllowCertificatePolicy.Instance,
                    ClientCertificateMode.RequireCertificate => RequireCertificatePolicy.Instance,
                    _ => validationPolicy ?? NoCertificatePolicy.Instance,
                };

                bool ValidateCertificate(object _, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => policy.Apply(cert, chain, errors);

                var clientCertificateRequired = policy is not NoCertificatePolicy and not AllowCertificatePolicy;

                if (certOptions.Path is not null)
                {
                    var path = environment.ContentRootFileProvider.GetFileInfo(certOptions.Path).PhysicalPath;
                    var keyPath = environment.ContentRootFileProvider.GetFileInfo(certOptions.KeyPath).PhysicalPath;
                    var password = certOptions.Password;

                    options.UseSslEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")),
                        protocols, () => CertificateLoader.LoadFromFile(path, keyPath, password),
                        ValidateCertificate, clientCertificateRequired);
                }
                else if (certOptions.Subject is not null)
                {
                    var store = certOptions.Store;
                    var location = certOptions.Location;
                    var subject = certOptions.Subject;
                    var allowInvalid = certOptions.AllowInvalid;

                    options.UseSslEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")),
                        protocols, () => CertificateLoader.LoadFromStore(store, location, subject, allowInvalid),
                        ValidateCertificate, clientCertificateRequired);
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