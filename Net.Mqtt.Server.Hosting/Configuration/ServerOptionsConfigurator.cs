using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Net.Mqtt.Server.Hosting.Configuration;

public class ServerOptionsConfigurator(IConfiguration configuration) : IConfigureOptions<ServerOptions>
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServerOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MqttOptions5))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Endpoint))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CertificateOptions))]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public void Configure([NotNull] ServerOptions options)
    {
        configuration.Bind(options);

        var endpoints = configuration.GetSection("Endpoints");
        var certificates = configuration.GetSection("Certificates");
        var map = certificates.Get<Dictionary<string, CertificateOptions>>();

        foreach (var (name, value) in options.Endpoints)
        {
            if (value is { Certificate: null })
            {
                // This might be missing value because "Certificate" was specified
                // as string reference to the item in the "Certificates" section.
                if (endpoints.GetValue<string>($"{name}:Certificate") is { } certName)
                {
                    if (map.TryGetValue(certName, out var certificate))
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