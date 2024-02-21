using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using OOs.Net.Connections;
using OOs.Net.Listeners;
using OOs.Net.Sockets;

namespace Net.Mqtt.Server.Hosting.Configuration;

public static class ListenerFactoryExtensions
{
    private static readonly string[] subProtocols = ["mqtt", "mqttv3.1"];

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateTcpSslListenerFactory(Uri uri,
            SslProtocols enabledSslProtocols, Func<X509Certificate2> certificateLoader,
            RemoteCertificateValidationCallback validationCallback, bool clientCertificateRequired)
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

    public static Func<IAsyncEnumerable<NetworkConnection>> CreateListenerFactory(Uri uri) => uri switch
    {
        { Scheme: "tcp" or "mqtt" } => () => new TcpSocketListener(new(IPAddress.Parse(uri.Host), uri.Port)),
        { Scheme: "unix" } or { IsFile: true } => () => new UnixDomainSocketListener(SocketBuilderExtensions.ResolveUnixDomainSocketPath(uri.LocalPath)),
        { Scheme: "ws" or "http", Host: "0.0.0.0" or "[::]" } u => () => new WebSocketListener([$"http://+:{u.Port}{u.PathAndQuery}"], subProtocols),
        { Scheme: "ws" or "http" } u => () => new WebSocketListener([$"http://{u.Authority}{u.PathAndQuery}"], subProtocols),
        _ => ThrowSchemaNotSupported()
    };

    [DoesNotReturn]
    private static Func<IAsyncEnumerable<NetworkConnection>> ThrowSchemaNotSupported() =>
        throw new ArgumentException("Uri schema is not supported.");
}