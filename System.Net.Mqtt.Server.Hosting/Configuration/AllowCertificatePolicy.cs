using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Security.SslPolicyErrors;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class AllowCertificatePolicy : ICertificateValidationPolicy
{
    public bool Apply(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return sslPolicyErrors is None or RemoteCertificateNotAvailable;
    }
}