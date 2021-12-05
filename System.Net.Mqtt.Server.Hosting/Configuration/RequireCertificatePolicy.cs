using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Security.SslPolicyErrors;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class RequireCertificatePolicy : ICertificateValidationPolicy
{
    private static RequireCertificatePolicy instance;

    public bool Apply(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return certificate is not null && chain is not null && sslPolicyErrors is None;
    }

    public static RequireCertificatePolicy Instance => instance ??= new RequireCertificatePolicy();
}