using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace Net.Mqtt.Server.Hosting;

public class MqttEndpointBuilder
{
    internal MqttEndpointBuilder(MqttEndpoint endPoint) => EndPoint = endPoint;

    internal MqttEndpoint EndPoint { get; }

    public void UseCertificate(CertificateOptions options) => EndPoint.Certificate = options;

    public void UseQuic() => EndPoint.UseQuic = true;

    public void UseCertificate(X509Certificate2 certificate) => EndPoint.Certificate =
        new CertificateOptions(() => certificate.CopyWithPrivateKey(certificate.GetECDiffieHellmanPrivateKey()!));

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
}