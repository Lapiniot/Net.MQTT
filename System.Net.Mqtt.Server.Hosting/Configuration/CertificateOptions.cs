using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public record CertificateOptions
{
    public StoreLocation Location { get; init; } = StoreLocation.CurrentUser;
    public StoreName Store { get; init; } = StoreName.My;
    public string Subject { get; init; }
    public string Path { get; init; }
    public string KeyPath { get; init; }
    public string Password { get; init; }
    public bool AllowInvalid { get; init; }
}