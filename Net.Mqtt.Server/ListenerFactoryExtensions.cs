using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using OOs.Net.Listeners;
using OOs.Net.Sockets;

namespace Net.Mqtt.Server;

public static class ListenerFactoryExtensions
{
    private static readonly string[] subProtocols = ["mqtt", "mqttv3.1"];

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> CreateTcp(string host, int port) =>
        () => new TcpSocketListener(new(IPAddress.Parse(host), port));

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> CreateTcp(IPAddress address, int port) =>
        () => new TcpSocketListener(new(address, port));

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> CreateTcp(IPEndPoint endPoint) =>
        () => new TcpSocketListener(endPoint);

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> CreateTcpSsl(
        IPEndPoint endPoint, SslProtocols enabledSslProtocols, Func<X509Certificate2> certificateLoader,
        RemoteCertificateValidationCallback? validationCallback, bool clientCertificateRequired)
    {
        return () =>
        {
            var serverCertificate = certificateLoader();

            try
            {
                return new TcpSslSocketListener(endPoint,
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

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> CreateWebSocket(string[] prefixes, string[]? subProtocols = null) =>
        () => new WebSocketListener(prefixes, subProtocols ?? ListenerFactoryExtensions.subProtocols);

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> CreateUnixDomainSocket(string path) =>
        () => new UnixDomainSocketListener(SocketBuilderExtensions.ResolveUnixDomainSocketPath(path));

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> Create(EndPoint endPoint) => endPoint switch
    {
        IPEndPoint ipEP => () => new TcpSocketListener(ipEP),
        UnixDomainSocketEndPoint udsEP => () => new UnixDomainSocketListener(udsEP),
        _ => ThrowEndPointTypeNotSupported(endPoint.GetType()),
    };

    public static Func<IAsyncEnumerable<NetworkTransportPipe>> Create(Uri uri) => uri switch
    {
        { Scheme: "tcp" or "mqtt", Host: var host, Port: var port } => CreateTcp(host, port > 0 ? port : 1883),
        ({ Scheme: "unix" } or { IsFile: true }) and { LocalPath: var path } => CreateUnixDomainSocket(path),
        { Scheme: "ws" or "http", Host: "0.0.0.0" or "[::]", Port: var port, PathAndQuery: var pathAndQuery } =>
            CreateWebSocket([$"http://+:{port}{pathAndQuery}"], subProtocols),
        { Scheme: "ws" or "http", Authority: var authority, PathAndQuery: var pathAndQuery } =>
            CreateWebSocket([$"http://{authority}{pathAndQuery}"], subProtocols),
        _ => ThrowSchemaNotSupported(uri.Scheme)
    };

    [DoesNotReturn]
    private static Func<IAsyncEnumerable<NetworkTransportPipe>> ThrowEndPointTypeNotSupported(Type type) =>
        throw new NotSupportedException($"'{type}' is not supported.");

    [DoesNotReturn]
    private static Func<IAsyncEnumerable<NetworkTransportPipe>> ThrowSchemaNotSupported(string scheme) =>
        throw new NotSupportedException($"Uri schema '{scheme}' is not supported.");
}