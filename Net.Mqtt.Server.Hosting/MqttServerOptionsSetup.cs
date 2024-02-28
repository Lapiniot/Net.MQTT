using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using static System.Environment;

#nullable enable

namespace Net.Mqtt.Server.Hosting;

internal sealed class MqttServerOptionsSetup(IConfiguration configuration) : IConfigureOptions<MqttServerOptions>
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MqttServerOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MqttOptions5))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MqttEndpoint))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CertificateOptions))]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public void Configure([NotNull] MqttServerOptions options)
    {
        // Try to populate MQTT5 props from root configuration 
        // in order to inherit defaults from generic server props
        configuration.Bind(options.MQTT5);

        // Then process normally so this will populate generic server props 
        // and then override MQTT5 specific props from MQTT5 subsection if latter is present
        configuration.Bind(options);

        // Manually patch some configuration properties (cert references by name, file path with env. variables expansion e.g.)
        // as far as automatic binding doesn't provide enough extensibility like value conversions, custom parsing from string etc.
        foreach (var (_, certificate) in options.Certificates)
        {
            ExpandEnvVars(certificate);
        }

        var endpointsSection = configuration.GetSection("Endpoints");
        foreach (var (name, endpoint) in options.Endpoints)
        {
            if (endpoint.Url is ({ IsFile: true } or { Scheme: "unix" }) and { OriginalString: var originalString })
            {
                endpoint.Url = new Uri(ExpandEnvironmentVariables(originalString));
            }

            if (endpoint is { Certificate: null })
            {
                // This might be missing value because "Certificate" was specified
                // as string reference to the item in the "Certificates" section.
                if (endpointsSection.GetValue<string>($"{name}:Certificate") is { } certName)
                {
                    if (options.Certificates.TryGetValue(certName, out var certificate))
                    {
                        endpoint.Certificate = certificate;
                    }
                    else
                    {
                        ThrowMissingCertificateConfiguration(certName);
                    }
                }
            }
            else
            {
                ExpandEnvVars(endpoint.Certificate);
            }
        }

        static void ExpandEnvVars(CertificateOptions certificate)
        {
            if (certificate is { Path: var path, KeyPath: var keyPath })
            {
                if (path is { })
                    certificate.Path = ExpandEnvironmentVariables(path);
                if (keyPath is { })
                    certificate.KeyPath = ExpandEnvironmentVariables(keyPath);
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowMissingCertificateConfiguration(string certName) =>
        throw new InvalidOperationException($"Certificate configuration for '{certName}' is missing.");
}