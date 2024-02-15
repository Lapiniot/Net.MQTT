using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Net.Mqtt.Server.Hosting;

public interface ICertificateValidationPolicy
{
    bool Apply(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);
}