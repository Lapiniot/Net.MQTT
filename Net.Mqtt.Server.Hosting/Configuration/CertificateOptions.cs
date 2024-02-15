using System.Security.Cryptography.X509Certificates;

namespace Net.Mqtt.Server.Hosting.Configuration;

public sealed class CertificateOptions
{
    public StoreLocation Location { get; set; } = StoreLocation.CurrentUser;
    public StoreName Store { get; set; } = StoreName.My;
    public string Subject { get; set; }
    public string Path { get; set; }
    public string KeyPath { get; set; }
    public string Password { get; set; }
    public bool AllowInvalid { get; set; }
}