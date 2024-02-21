using System.Security.Authentication;
using OOs.Net.Connections;

namespace Net.Mqtt.Server.Hosting.Configuration;

public class Endpoint
{
    private readonly Func<IAsyncEnumerable<NetworkConnection>> factory;

    public Endpoint() { }

    public Endpoint(Func<IAsyncEnumerable<NetworkConnection>> factory) => this.factory = factory;

    public string Url { get; set; }
    public CertificateOptions Certificate { get; set; }
    public SslProtocols SslProtocols { get; set; } = SslProtocols.None;
    public ClientCertificateMode? ClientCertificateMode { get; set; }

#pragma warning disable CA1024 // Use properties where appropriate
    // This is just a dumb workaround to calm down the ConfigurationBindingGenerator 
    // which doesn't skip property binding with unsupported types. 
    // Otherwise this would be a simple readonly property.
    // TODO: check necessity of this trick in the upcoming .NET releases!
    public Func<IAsyncEnumerable<NetworkConnection>> GetFactory() => factory;
#pragma warning restore CA1024 // Use properties where appropriate
}