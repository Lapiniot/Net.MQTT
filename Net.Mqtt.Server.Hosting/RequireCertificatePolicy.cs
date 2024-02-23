using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Security.SslPolicyErrors;

namespace Net.Mqtt.Server.Hosting;

public sealed class RequireCertificatePolicy : IRemoteCertificateValidationPolicy
{
    private static RequireCertificatePolicy instance;

    public static RequireCertificatePolicy Instance => instance ??= new();

    public bool Required => true;

    public bool Verify(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
        certificate is not null && chain is not null && sslPolicyErrors is None;
}