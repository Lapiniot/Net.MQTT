using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace Net.Mqtt.Server.Hosting;

public class MqttEndpointBuilder
{
    private readonly MqttServerOptions options;

    internal MqttEndpointBuilder(MqttEndpoint endPoint, MqttServerOptions options)
    {
        this.options = options;
        EndPoint = endPoint;
    }

    internal MqttEndpoint EndPoint { get; }

    public void UseCertificate(CertificateOptions options) => EndPoint.Certificate = options;

    public void UseCertificate(string name)
    {
        if (options.Certificates.TryGetValue(name, out var certOptions))
        {
            EndPoint.Certificate = certOptions;
        }
        else
        {
            ThrowCertificateConfigurationNotFound(name);
        }
    }

    public void UseCertificate(X509Certificate2 certificate) =>
        EndPoint.Certificate = new CertificateOptions(() => certificate);

    public void UseCertificate(Func<X509Certificate2> loader) =>
        EndPoint.Certificate = new CertificateOptions(loader);

    public SslProtocols SslProtocols
    {
        get => EndPoint.SslProtocols;
        set => EndPoint.SslProtocols = value;
    }

    public ClientCertificateMode? ClientCertificateMode
    {
        get => EndPoint.ClientCertificateMode;
        set => EndPoint.ClientCertificateMode = value;
    }

    [DoesNotReturn]
    private static void ThrowCertificateConfigurationNotFound(string name) =>
        throw new InvalidOperationException($"Requested certificate '{name}' was not found in the configuration.");
}