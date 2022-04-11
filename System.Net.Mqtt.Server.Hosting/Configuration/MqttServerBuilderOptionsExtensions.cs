using System.Net.Connections;
using System.Net.Listeners;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using static System.Net.Mqtt.Server.Hosting.Configuration.ClientCertificateMode;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public static class MqttServerBuilderOptionsExtensions
{
    private static string[] subProtocols;

    public static MqttServerBuilderOptions UseEndpoint(this MqttServerBuilderOptions options, string name, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ListenerFactories.Add(name, _ => CreateListener(uri));

        return options;
    }

    public static MqttServerBuilderOptions UseSslEndpoint(this MqttServerBuilderOptions options, string name, Uri uri,
        Func<X509Certificate2> certificateLoader, SslProtocols enabledSslProtocols = SslProtocols.None,
        ClientCertificateMode certificateMode = NoCertificate)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ListenerFactories.Add(name, provider =>
        {
            var serverCertificate = certificateLoader();

            try
            {
                var policy = provider.GetService<ICertificateValidationPolicy>() ?? certificateMode switch
                {
                    NoCertificate => NoCertificatePolicy.Instance,
                    AllowCertificate => AllowCertificatePolicy.Instance,
                    RequireCertificate => RequireCertificatePolicy.Instance,
                    _ => throw new NotImplementedException()
                };

                return new TcpSslSocketListener(new(IPAddress.Parse(uri.Host), uri.Port),
                    serverCertificate: serverCertificate, enabledSslProtocols: enabledSslProtocols,
                    remoteCertificateValidationCallback: (_, cert, chain, errors) => policy.Apply(cert, chain, errors),
                    clientCertificateRequired: certificateMode is RequireCertificate or AllowCertificate);
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
            _ => throw new ArgumentException("Uri schema not supported.")
        };

    private static string[] SubProtocols => subProtocols ??= new[] { "mqtt", "mqttv3.1" };
}