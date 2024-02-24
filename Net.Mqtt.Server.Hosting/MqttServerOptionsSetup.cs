using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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

        var endpoints = configuration.GetSection("Endpoints");
        foreach (var (name, value) in options.Endpoints)
        {
            if (value is { Certificate: null })
            {
                // This might be missing value because "Certificate" was specified
                // as string reference to the item in the "Certificates" section.
                if (endpoints.GetValue<string>($"{name}:Certificate") is { } certName)
                {
                    if (options.Certificates.TryGetValue(certName, out var certificate))
                    {
                        value.Certificate = certificate;
                    }
                    else
                    {
                        ThrowMissingCertificateConfiguration(certName);
                    }
                }
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowMissingCertificateConfiguration(string certName) =>
        throw new InvalidOperationException($"Certificate configuration for '{certName}' is missing.");
}