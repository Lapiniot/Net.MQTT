using System.Net.Connections;
using System.Net.Mqtt.Properties;
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
            _ => throw new ArgumentException(Strings.SchemaNotSupported)
        };
    }

#pragma warning disable CA2000 // Dispose objects before losing scope - Ownership is transfered to the wrapping NetworkTransport instance

    public static NetworkTransport CreateWebSockets(Uri uri, string[] subProtocols = null,
        X509Certificate2[] clientCertificates = null, TimeSpan? keepAliveInterval = null)
    {
        uri = uri switch
        {
            null => throw new ArgumentNullException(nameof(uri)),
            { Scheme: "ws" or "wss" } => uri,
            { Scheme: "http" } => new UriBuilder(uri) { Scheme = "ws" }.Uri,
            { Scheme: "https" } => new UriBuilder(uri) { Scheme = "wss" }.Uri,
            _ => throw new ArgumentException(Strings.SchemaNotSupported),
        };

        return new NetworkConnectionAdapterTransport(
            new WebSocketClientConnection(uri, subProtocols ?? defaultSubProtocols, clientCertificates, keepAliveInterval), true);
    }

    public static NetworkTransport CreateTcp(IPEndPoint endPoint)
    {
        return new NetworkConnectionAdapterTransport(new TcpClientSocketConnection(endPoint), true);
    }

    public static NetworkTransport CreateTcp(IPAddress address, int port)
    {
        return CreateTcp(new IPEndPoint(address, port));
    }

    public static NetworkTransport CreateTcp(string hostNameOrAddress, int port)
    {
        return new NetworkConnectionAdapterTransport(new TcpClientSocketConnection(hostNameOrAddress, port), true);
    }

    public static NetworkTransport CreateTcpSsl(IPEndPoint endPoint,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate2[] certificates = null)
    {
        return new NetworkConnectionAdapterTransport(new TcpSslClientSocketConnection(endPoint, machineName, enabledSslProtocols, certificates), true);
    }

    public static NetworkTransport CreateTcpSsl(IPAddress address, int port,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate2[] certificates = null)
    {
        return CreateTcpSsl(new IPEndPoint(address, port), machineName, enabledSslProtocols, certificates);
    }

    public static NetworkTransport CreateTcpSsl(string hostNameOrAddress, int port,
        string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate2[] certificates = null)
    {
        return new NetworkConnectionAdapterTransport(
            new TcpSslClientSocketConnection(hostNameOrAddress, port, machineName, enabledSslProtocols, certificates), true);
    }

#pragma warning restore
}