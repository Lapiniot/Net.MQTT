using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Mqtt.Server.Hosting.Configuration;
using OOs.Net.Connections;

namespace Net.Mqtt.Server.Hosting;

internal static class OptionsMapper
{
    public static ServerOptions Map(this MqttServerOptions options) => new()
    {
        ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeoutMilliseconds),
        Protocols = (MqttProtocol)options.ProtocolLevel,
        MaxInFlight = options.MaxInFlight ?? (ushort)short.MaxValue,
        MaxReceive = options.MaxReceive ?? (ushort)short.MaxValue,
        MaxUnflushedBytes = options.MaxUnflushedBytes ?? int.MaxValue,
        MaxPacketSize = options.MaxPacketSize ?? int.MaxValue,
        MQTT5 = new()
        {
            MaxInFlight = options.MQTT5?.MaxInFlight ?? options.MaxInFlight ?? (ushort)short.MaxValue,
            MaxReceive = options.MQTT5?.MaxReceive ?? options.MaxReceive ?? (ushort)short.MaxValue,
            MaxUnflushedBytes = options.MQTT5?.MaxUnflushedBytes ?? options.MaxUnflushedBytes ?? int.MaxValue,
            MaxPacketSize = options.MQTT5?.MaxPacketSize ?? options.MaxPacketSize ?? int.MaxValue,
            TopicAliasSizeThreshold = options.MQTT5?.TopicAliasSizeThreshold ?? 128
        }
    };

    public static IReadOnlyDictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> Map(
        this IReadOnlyDictionary<string, Endpoint> endpoints, IServiceProvider serviceProvider)
    {
        var mapped = new Dictionary<string, Func<IAsyncEnumerable<NetworkConnection>>>();

        foreach (var (name, ep) in endpoints)
        {
            if (ep.GetFactory() is { } factory)
            {
                mapped.Add(name, factory);
                continue;
            }

            switch (ep)
            {
                case { Url: { } url, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateListenerFactory(new(Expand(url))));
                    break;
                case
                {
                    Url: { } url, Certificate: { } cert, ClientCertificateMode: var certMode,
                    SslProtocols: var sslProtocols
                }:
                    {
                        var policy = certMode switch
                        {
                            ClientCertificateMode.NoCertificate => NoCertificatePolicy.Instance,
                            ClientCertificateMode.AllowCertificate => AllowCertificatePolicy.Instance,
                            ClientCertificateMode.RequireCertificate => RequireCertificatePolicy.Instance,
                            _ => serviceProvider.GetService<ICertificateValidationPolicy>() ?? NoCertificatePolicy.Instance
                        };

                        bool ValidateCertificate(object _, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) =>
                            policy.Apply(cert, chain, errors);

                        var rootPath = serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;

                        var certificateLoader = cert switch
                        {
                            { Path: { } certPath, KeyPath: var certKeyPath, Password: var password } =>
                                () => CertificateLoader.LoadFromFile(
                                    path: Path.Combine(rootPath, Expand(certPath)),
                                    keyPath: !string.IsNullOrEmpty(Expand(certKeyPath)) ? Path.Combine(rootPath, certKeyPath) : null,
                                    password),
                            { Subject: { } subj, Store: var store, Location: var location, AllowInvalid: var allowInvalid } =>
                                () => CertificateLoader.LoadFromStore(store, location, subj, allowInvalid),
                            _ => ThrowCannotLoadCertificate(),
                        };

                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSslListenerFactory(new Uri(url),
                            sslProtocols, certificateLoader, ValidateCertificate,
                            clientCertificateRequired: policy is not NoCertificatePolicy and not AllowCertificatePolicy));
                    }

                    break;
            }
        }

        return mapped;
    }

    private static string Expand(string value) => Environment.ExpandEnvironmentVariables(value);

    [DoesNotReturn]
    private static Func<X509Certificate2> ThrowCannotLoadCertificate() => throw new InvalidOperationException(
            "Cannot load certificate from configuration. Either store information or file path should be provided.");
}