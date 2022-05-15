using System.Diagnostics.CodeAnalysis;
using System.Net.Connections;
using System.Net.Listeners;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public static class MqttServerBuilderOptionsExtensions
{
    private static string[] subProtocols;

    public static MqttServerBuilderOptions UseEndpoint(this MqttServerBuilderOptions options, string name, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ListenerFactories.Add(name, () => CreateListener(uri));

        return options;
    }

    public static MqttServerBuilderOptions UseSslEndpoint(this MqttServerBuilderOptions options, string name, Uri uri,
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
            { Scheme: "ws" or "http", Host: "0.0.0.0" or "[::]" } u => new WebSocketListener(new[] { $"http://+:{u.Port}{u.PathAndQuery}" }, SubProtocols),
            { Scheme: "ws" or "http" } u => new WebSocketListener(new[] { $"http://{u.Authority}{u.PathAndQuery}" }, SubProtocols),
            _ => ThrowSchemaNotSupported()
        };

    private static string[] SubProtocols => subProtocols ??= new[] { "mqtt", "mqttv3.1" };

    [DoesNotReturn]
    private static IAsyncEnumerable<NetworkConnection> ThrowSchemaNotSupported() =>
        throw new ArgumentException("Uri schema is not supported.");
}