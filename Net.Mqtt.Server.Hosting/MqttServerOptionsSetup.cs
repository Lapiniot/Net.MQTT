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
        foreach (var (name, certificate) in options.Certificates)
        {
            if (certificate is { Path: { } path, KeyPath: var keyPath })
            {
                options.Certificates[name] = (certificate with
                {
                    Path = ExpandEnvironmentVariables(path),
                    KeyPath = keyPath is { } ? ExpandEnvironmentVariables(keyPath) : keyPath
                });
            }
        }

        foreach (var (_, endpoint) in options.Endpoints)
        {
            if (endpoint.Url is ({ IsFile: true } or { Scheme: "unix" }) and { OriginalString: var originalString })
            {
                endpoint.Url = new Uri(ExpandEnvironmentVariables(originalString));
            }
        }
    }
}