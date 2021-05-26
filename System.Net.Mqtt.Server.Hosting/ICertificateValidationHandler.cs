using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt.Server.Hosting
{
    public interface ICertificateValidationHandler
    {
        bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);
    }
}