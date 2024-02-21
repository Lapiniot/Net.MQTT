using System.Security.Authentication;

namespace Net.Mqtt.Server.Hosting.Configuration;

public class Endpoint
{
    public Uri Url { get; set; }
    public CertificateOptions Certificate { get; set; }
    public SslProtocols SslProtocols { get; set; } = SslProtocols.None;
    public ClientCertificateMode? ClientCertificateMode { get; set; }
}