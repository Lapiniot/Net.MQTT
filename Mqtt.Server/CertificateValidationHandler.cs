using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mqtt.Server.Hosting;

namespace Mqtt.Server;

public class CertificateValidationHandler : ICertificateValidationHandler
{
    public bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }
}