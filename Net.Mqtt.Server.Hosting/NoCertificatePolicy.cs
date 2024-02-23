using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Net.Mqtt.Server.Hosting;

public sealed class NoCertificatePolicy : IRemoteCertificateValidationPolicy
{
    private static NoCertificatePolicy instance;

    public static NoCertificatePolicy Instance => instance ??= new();

    public bool Required => false;

    public bool Verify(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
}