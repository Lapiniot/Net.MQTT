using System.Diagnostics.CodeAnalysis;
using System.Net.Connections;
using System.Net.Listeners;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public static class ServerOptionsExtensions
{
    private static readonly string[] subProtocols = ["mqtt", "mqttv3.1"];

    public static ServerOptions UseEndpoint(this ServerOptions options, string name, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ListenerFactories.Add(name, () => CreateListener(uri));
        return options;
    }

    public static ServerOptions UseSslEndpoint(this ServerOptions options, string name, Uri uri,
        SslProtocols enabledSslProtocols, Func<X509Certificate2> certificateLoader,
        RemoteCertificateValidationCallback validationCallback, bool clientCertificateRequired)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ListenerFactories.Add(name, () =>
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
        });

        return options;
    }

    private static IAsyncEnumerable<NetworkConnection> CreateListener(Uri uri) =>
        uri switch
        {
            { Scheme: "tcp" or "mqtt" } => new TcpSocketListener(new(IPAddress.Parse(uri.Host), uri.Port)),
            { Scheme: "unix" } or { IsFile: true } => new UnixDomainSocketListener(SocketBuilderExtensions.ResolveUnixDomainSocketPath(uri.LocalPath)),
            { Scheme: "ws" or "http", Host: "0.0.0.0" or "[::]" } u => new WebSocketListener([$"http://+:{u.Port}{u.PathAndQuery}"], subProtocols),
            { Scheme: "ws" or "http" } u => new WebSocketListener([$"http://{u.Authority}{u.PathAndQuery}"], subProtocols),
            _ => ThrowSchemaNotSupported()
        };

    [DoesNotReturn]
    private static IAsyncEnumerable<NetworkConnection> ThrowSchemaNotSupported() =>
        throw new ArgumentException("Uri schema is not supported.");
}