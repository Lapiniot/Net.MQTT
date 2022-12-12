using System.Net.Connections;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt;

public static class NetworkConnectionFactory
{
    private static readonly string[] DefaultSubProtocols = { "mqttv3.1", "mqtt" };

    public static NetworkConnection Create(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return uri switch
        {
            { Scheme: "ws" or "wss" or "http" or "https" } u => CreateWebSockets(u, null),
            { Scheme: "tcp", Host: var host, Port: var port } => CreateTcp(host, port),
            { Scheme: "tcps", Host: var host, Port: var port } => CreateTcpSsl(host, port),
            { Scheme: "unix", LocalPath: var unixPath } => CreateUnixDomain(new UnixDomainSocketEndPoint(unixPath)),
            _ => ThrowSchemaNotSupported<NetworkConnection>()
        };
    }

    public static NetworkConnection CreateWebSockets(Uri uri, Action<ClientWebSocketOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(uri);

        uri = uri switch
        {
            { Scheme: "ws" or "wss" } => uri,
            { Scheme: "http" } => new UriBuilder(uri) { Scheme = "ws" }.Uri,
            { Scheme: "https" } => new UriBuilder(uri) { Scheme = "wss" }.Uri,
            _ => ThrowSchemaNotSupported<Uri>()
        };

        void ConfigureOptions(ClientWebSocketOptions options)
        {
            ConfigureDefaultOptions(options);
            configureOptions?.Invoke(options);
        }

        return new WebSocketClientConnection(uri, ConfigureOptions);
    }

    public static NetworkConnection CreateTcp(IPEndPoint endPoint) => new TcpSocketClientConnection(endPoint);

    public static NetworkConnection CreateTcp(IPAddress address, int port) => CreateTcp(new(address, port));

    public static NetworkConnection CreateTcp(string hostNameOrAddress, int port) => new TcpSocketClientConnection(hostNameOrAddress, port);

    public static NetworkConnection CreateTcpSsl(IPEndPoint endPoint,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) => new TcpSslSocketClientConnection(endPoint, machineName, enabledSslProtocols, certificates);

    public static NetworkConnection CreateTcpSsl(IPAddress address, int port,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) => CreateTcpSsl(new(address, port), machineName, enabledSslProtocols, certificates);

    public static NetworkConnection CreateTcpSsl(string hostNameOrAddress, int port,
        string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) => new TcpSslSocketClientConnection(hostNameOrAddress, port, machineName, enabledSslProtocols, certificates);

    public static NetworkConnection CreateUnixDomain(UnixDomainSocketEndPoint endPoint) =>
        new UnixDomainSocketClientConnection(endPoint);

    private static void ConfigureDefaultOptions(ClientWebSocketOptions options)
    {
        foreach (var subProtocol in DefaultSubProtocols)
        {
            options.AddSubProtocol(subProtocol);
        }
    }

    [DoesNotReturn]
    internal static T ThrowSchemaNotSupported<T>() =>
        throw new ArgumentException("Uri schema is not supported.");
}