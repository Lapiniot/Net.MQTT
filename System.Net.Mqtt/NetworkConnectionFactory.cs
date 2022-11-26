using System.Net.Connections;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt;

public static class NetworkTransportFactory
{
    private static readonly string[] defaultSubProtocols = { "mqttv3.1", "mqtt" };

    public static NetworkTransport Create(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return uri switch
        {
            { Scheme: "ws" or "wss" or "http" or "https" } u => CreateWebSockets(u),
            { Scheme: "tcp", Host: var host, Port: var port } => CreateTcp(host, port),
            { Scheme: "tcps", Host: var host, Port: var port } => CreateTcpSsl(host, port),
            { Scheme: "unix", LocalPath: var unixPath } => CreateUnixDomain(new UnixDomainSocketEndPoint(unixPath)),
            _ => ThrowSchemaNotSupported<NetworkTransport>()
        };
    }

#pragma warning disable CA2000 // Dispose objects before losing scope - Ownership is transferred to the wrapping NetworkTransport instance

    public static NetworkTransport CreateWebSockets(Uri uri, string[] subProtocols = null,
        X509Certificate[] clientCertificates = null, TimeSpan? keepAliveInterval = null)
    {
        ArgumentNullException.ThrowIfNull(uri);

        uri = uri switch
        {
            { Scheme: "ws" or "wss" } => uri,
            { Scheme: "http" } => new UriBuilder(uri) { Scheme = "ws" }.Uri,
            { Scheme: "https" } => new UriBuilder(uri) { Scheme = "wss" }.Uri,
            _ => ThrowSchemaNotSupported<Uri>()
        };

        return new NetworkTransport(new WebSocketClientConnection(uri, subProtocols ?? defaultSubProtocols, clientCertificates, keepAliveInterval));
    }

    public static NetworkTransport CreateTcp(IPEndPoint endPoint) => new(new TcpClientSocketConnection(endPoint));

    public static NetworkTransport CreateTcp(IPAddress address, int port) => CreateTcp(new(address, port));

    public static NetworkTransport CreateTcp(string hostNameOrAddress, int port) => new(new TcpClientSocketConnection(hostNameOrAddress, port));

    public static NetworkTransport CreateTcpSsl(IPEndPoint endPoint,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) => new(new TcpSslClientSocketConnection(endPoint, machineName, enabledSslProtocols, certificates));

    public static NetworkTransport CreateTcpSsl(IPAddress address, int port,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) => CreateTcpSsl(new(address, port), machineName, enabledSslProtocols, certificates);

    public static NetworkTransport CreateTcpSsl(string hostNameOrAddress, int port,
        string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) => new(new TcpSslClientSocketConnection(hostNameOrAddress, port, machineName, enabledSslProtocols, certificates));

    public static NetworkTransport CreateUnixDomain(UnixDomainSocketEndPoint endPoint) =>
        new(new UnixDomainClientSocketConnection(endPoint));

#pragma warning restore

    [DoesNotReturn]
    internal static T ThrowSchemaNotSupported<T>() =>
        throw new ArgumentException("Uri schema is not supported.");
}