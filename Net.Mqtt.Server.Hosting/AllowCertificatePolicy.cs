using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Security.SslPolicyErrors;

namespace Net.Mqtt.Server.Hosting;

public sealed class AllowCertificatePolicy : ICertificateValidationPolicy
{
    private static AllowCertificatePolicy instance;

    public static AllowCertificatePolicy Instance => instance ??= new();

    public bool Apply(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
        sslPolicyErrors is None or RemoteCertificateNotAvailable;
}