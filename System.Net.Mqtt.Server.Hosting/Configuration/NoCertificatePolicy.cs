using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class NoCertificatePolicy : ICertificateValidationPolicy
{
    public bool Apply(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }
}