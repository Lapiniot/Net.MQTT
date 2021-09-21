using System.Net.Connections;
using System.Net.Listeners;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public static class MqttServerBuilderOptionsExtensions
{
    private static string[] subProtocols;

    public static MqttServerBuilderOptions UseEndpoint(this MqttServerBuilderOptions options, string name, Uri uri)
    {
        if(options is null) throw new ArgumentNullException(nameof(options));

        options.ListenerFactories.Add(name, _ => CreateListener(uri));

        return options;
    }

    public static MqttServerBuilderOptions UseSslEndpoint(this MqttServerBuilderOptions options, string name, Uri uri,
        Func<X509Certificate2> certificateLoader, SslProtocols enabledSslProtocols = SslProtocols.None)
    {
        if(options is null) throw new ArgumentNullException(nameof(options));

        options.ListenerFactories.Add(name, provider =>
        {
            var serverCertificate = certificateLoader();

            try
            {
                var handler = provider.GetService<ICertificateValidationHandler>();
                return new SslStreamTcpSocketListener(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port),
                    serverCertificate: serverCertificate, enabledSslProtocols: enabledSslProtocols,
                    remoteCertificateValidationCallback: handler is not null ? handler.Validate : null);
            }
            catch
            {
                serverCertificate.Dispose();
                throw;
            }
        });

        return options;
    }

    private static IAsyncEnumerable<INetworkConnection> CreateListener(Uri uri)
    {
        return uri switch
        {
            { Scheme: "tcp" } => new TcpSocketListener(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port)),
            { Scheme: "http", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"{u.Scheme}://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
            { Scheme: "http" } u => new WebSocketListener(new[] { $"{u.Scheme}://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
            { Scheme: "ws", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"http://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
            { Scheme: "ws" } u => new WebSocketListener(new[] { $"http://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
            _ => throw new ArgumentException("Uri schema not supported.")
        };
    }

    private static string[] GetSubProtocols()
    {
        return subProtocols ??= new string[] { "mqtt", "mqttv3.1" };
    }
}