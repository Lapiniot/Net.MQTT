using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public class NoCertificatePolicy : ICertificateValidationPolicy
{
    private static NoCertificatePolicy instance;

    public bool Apply(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

    public static NoCertificatePolicy Instance => instance ??= new NoCertificatePolicy();
}