using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OOs.Net.Connections;

namespace Net.Mqtt.Server.Hosting;

internal static class OptionsMapper
{
    public static ServerOptions Map(this MqttServerOptions options) => new()
    {
        ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeoutMilliseconds),
        Protocols = (MqttProtocol)options.ProtocolLevel,
        MaxInFlight = options.MaxInFlight,
        MaxReceive = options.MaxReceive,
        MaxUnflushedBytes = options.MaxUnflushedBytes,
        MaxPacketSize = options.MaxPacketSize,
        MQTT5 = new()
        {
            MaxInFlight = options.MQTT5.MaxInFlight,
            MaxReceive = options.MQTT5.MaxReceive,
            MaxUnflushedBytes = options.MQTT5.MaxUnflushedBytes,
            MaxPacketSize = options.MQTT5.MaxPacketSize,
            TopicAliasSizeThreshold = options.MQTT5.TopicAliasSizeThreshold,
            TopicAliasMax = options.MQTT5.TopicAliasMax
        }
    };

    public static IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> Map(
        this IReadOnlyDictionary<string, MqttEndpoint> endpoints, IServiceProvider serviceProvider)
    {
        var mapped = new Dictionary<string, Func<IAsyncEnumerable<NetworkConnection>>>();
        var rootPath = serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;

        foreach (var (name, ep) in endpoints)
        {
            if (ep.GetFactory() is { } factory)
            {
                mapped.Add(name, factory);
                continue;
            }

            switch (ep)
            {
                case { EndPoint: { } endPoint, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.Create(endPoint));
                    break;
                case { Url: { } url, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.Create(new Uri(Expand(url))));
                    break;
                case { Url: { } url, Certificate: { } cert, ClientCertificateMode: var certMode, SslProtocols: var sslProtocols }:
                    {
                        var uri = new Uri(url);
                        var policy = ResolveCertPolicy(serviceProvider, certMode);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port), sslProtocols,
                            certificateLoader: cert.GetLoader() ?? cert.Map(rootPath),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;
                case { EndPoint: IPEndPoint endpoint, Certificate: { } cert, ClientCertificateMode: var certMode, SslProtocols: var sslProtocols }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, certMode);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            endpoint, sslProtocols,
                            certificateLoader: cert.GetLoader() ?? cert.Map(rootPath),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;
            }
        }

        return mapped;

        static IRemoteCertificateValidationPolicy ResolveCertPolicy(IServiceProvider serviceProvider, ClientCertificateMode? certMode)
        {
            return certMode switch
            {
                ClientCertificateMode.NoCertificate => NoCertificatePolicy.Instance,
                ClientCertificateMode.AllowCertificate => AllowCertificatePolicy.Instance,
                ClientCertificateMode.RequireCertificate => RequireCertificatePolicy.Instance,
                _ => serviceProvider.GetService<IRemoteCertificateValidationPolicy>() ?? NoCertificatePolicy.Instance
            };
        }
    }

    public static Func<X509Certificate2> Map(this CertificateOptions certificate, string rootPath)
    {
        return certificate switch
        {
            { Path: { } certPath, KeyPath: var certKeyPath, Password: var password } =>
                () => CertificateLoader.LoadFromFile(
                    path: Path.Combine(rootPath, Expand(certPath)),
                    keyPath: !string.IsNullOrEmpty(certKeyPath) ? Path.Combine(rootPath, Expand(certKeyPath)) : null,
                    password),
            { Subject: { } subj, Store: var store, Location: var location, AllowInvalid: var allowInvalid } =>
                () => CertificateLoader.LoadFromStore(store, location, subj, allowInvalid),
            _ => ThrowCannotLoadCertificate(),
        };
    }

    private static string Expand(string value) => Environment.ExpandEnvironmentVariables(value);

    [DoesNotReturn]
    private static Func<X509Certificate2> ThrowCannotLoadCertificate() => throw new InvalidOperationException(
        "Cannot load certificate from configuration. Either store information or file path should be provided.");
}