using OOs.Net.Listeners;
using OOs.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Net.Mqtt.Server;

public static class ListenerFactoryExtensions
{
    private static readonly string[] subProtocols = ["mqtt", "mqttv3.1"];

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateTcpListenerFactory(string host, int port) =>
        () => new TcpSocketListener(new(IPAddress.Parse(host), port));

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateTcpListenerFactory(IPAddress address, int port) =>
        () => new TcpSocketListener(new(address, port));

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateTcpListenerFactory(IPEndPoint endPoint) =>
        () => new TcpSocketListener(endPoint);

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateTcpSslListenerFactory(Uri uri, SslProtocols enabledSslProtocols,
        Func<X509Certificate2> certificateLoader, RemoteCertificateValidationCallback validationCallback, bool clientCertificateRequired)
    {
        return () =>
        {
            var serverCertificate = certificateLoader();

            try
            {
                return new TcpSslSocketListener(new(IPAddress.Parse(uri.Host), uri.Port),
                    serverCertificate: serverCertificate, enabledSslProtocols: enabledSslProtocols,
                    remoteCertificateValidationCallback: validationCallback,
                    clientCertificateRequired: clientCertificateRequired);
            }
            catch
            {
                serverCertificate.Dispose();
                throw;
            }
        };
    }

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateWebSocketListenerFactory(string[] prefixes, string[] subProtocols) =>
        () => new WebSocketListener(prefixes, subProtocols);

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateUnixDomainSocketListenerFactory(string path) =>
        () => new UnixDomainSocketListener(SocketBuilderExtensions.ResolveUnixDomainSocketPath(path));

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateListenerFactory(Uri uri) => uri switch
    {
        { Scheme: "tcp" or "mqtt", Host: var host, Port: var port } => CreateTcpListenerFactory(host, port),
        ({ Scheme: "unix" } or { IsFile: true }) and { LocalPath: var path } => CreateUnixDomainSocketListenerFactory(path),
        { Scheme: "ws" or "http", Host: "0.0.0.0" or "[::]", Port: var port, PathAndQuery: var pathAndQuery } =>
            CreateWebSocketListenerFactory([$"http://+:{port}{pathAndQuery}"], subProtocols),
        { Scheme: "ws" or "http", Authority: var authority, PathAndQuery: var pathAndQuery } =>
            CreateWebSocketListenerFactory([$"http://{authority}{pathAndQuery}"], subProtocols),
        _ => ThrowSchemaNotSupported()
    };

    [DoesNotReturn]
    private static Func<IAsyncEnumerable<NetworkConnection>> ThrowSchemaNotSupported() =>
        throw new ArgumentException("Uri schema is not supported.");
}