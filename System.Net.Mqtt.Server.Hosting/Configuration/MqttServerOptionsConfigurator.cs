﻿using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class MqttServerOptionsConfigurator : IConfigureOptions<MqttServerOptions>
{
    internal const string RootSectionName = "MQTT";
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ICertificateValidationPolicy validationPolicy;

    public MqttServerOptionsConfigurator(IConfiguration configuration, IHostEnvironment environment,
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

        return protocols;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CertificateOptions))]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    private static CertificateOptions ResolveCertificateOptions(IConfigurationSection certificate, IConfiguration certificates)
    {
        var certName = certificate.Value;

        if (certName is null) return certificate.Get<CertificateOptions>();

        var certSection = certificates.GetSection(certName);

        if (!certSection.Exists())
            ThrowMissingConfiguration(certName);

        return certSection.Get<CertificateOptions>();
    }

    [DoesNotReturn]
    private static void ThrowMissingConfiguration(string certName) =>
        throw new InvalidOperationException($"Certificate configuration for '{certName}' is missing.");

    [DoesNotReturn]
    private static void ThrowCannotLoadCertificate() =>
        throw new InvalidOperationException("Cannot load certificate from configuration. Either store information or file path should be provided.");

    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public void Configure([NotNull] MqttServerOptions options)
    {
        var section = configuration.GetSection(RootSectionName);

        options.ConnectTimeout = section.GetValue("ConnectTimeout", 5000);
        options.MaxInFlight = section.GetValue("MaxInFlight", (ushort)short.MaxValue);
        options.ProtocolLevel = section.GetValue("ProtocolLevel", ProtocolLevel.All);
        options.MaxUnflushedBytes = section.GetValue("MaxUnflushedBytes", 4096);

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
                    _ => validationPolicy ?? NoCertificatePolicy.Instance
                };

                bool ValidateCertificate(object _, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => policy.Apply(cert, chain, errors);

                var clientCertificateRequired = policy is not NoCertificatePolicy and not AllowCertificatePolicy;

                if (certOptions.Path is not null)
                {
                    var path = environment.ContentRootFileProvider.GetFileInfo(certOptions.Path).PhysicalPath;
                    var keyPath = environment.ContentRootFileProvider.GetFileInfo(certOptions.KeyPath).PhysicalPath;
                    var password = certOptions.Password;

                    options.UseSslEndpoint(config.Key, new(GetUrl(config)),
                        protocols, () => CertificateLoader.LoadFromFile(path, keyPath, password),
                        ValidateCertificate, clientCertificateRequired);
                }
                else if (certOptions.Subject is not null)
                {
                    var store = certOptions.Store;
                    var location = certOptions.Location;
                    var subject = certOptions.Subject;
                    var allowInvalid = certOptions.AllowInvalid;

                    options.UseSslEndpoint(config.Key, new(GetUrl(config)),
                        protocols, () => CertificateLoader.LoadFromStore(store, location, subject, allowInvalid),
                        ValidateCertificate, clientCertificateRequired);
                }
                else
                {
                    ThrowCannotLoadCertificate();
                    return;
                }
            }
            else
            {
                options.UseEndpoint(config.Key, new(GetUrl(config)));
            }
        }

        static string GetUrl(IConfigurationSection config) => config.Value ?? config.GetValue<string>("Url");
    }
}