using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Net.Mqtt.Server.Hosting.Configuration;
using OOs.Net.Connections;

namespace Net.Mqtt.Server.Hosting;

internal static class OptionsMapper
{
    public static MqttServerOptions Map(this ServerOptions options) => new()
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
        this IReadOnlyDictionary<string, Endpoint> endpoints,
        IHostEnvironment environment,
        ICertificateValidationPolicy certificateValidationPolicy = null)
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
                    mapped.Add(name, ListenerFactoryExtensions.CreateListenerFactory(Resolve(url)));
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
                            _ => certificateValidationPolicy ?? NoCertificatePolicy.Instance
                        };

                        bool ValidateCertificate(object _, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
                        {
                            return policy.Apply(cert, chain, errors);
                        }

                        var certificateLoader = cert switch
                        {
                            { Path: { } certPath, KeyPath: var certKeyPath, Password: var password } =>
                                () => CertificateLoader.LoadFromFile(
                                    path: Path.Combine(environment.ContentRootPath, certPath),
                                    keyPath: !string.IsNullOrEmpty(certKeyPath)
                                        ? Path.Combine(environment.ContentRootPath, certKeyPath)
                                        : null,
                                    password),
                            { Subject: { } subj, Store: var store, Location: var location, AllowInvalid: var allowInvalid } =>
                                () => CertificateLoader.LoadFromStore(store, location, subj, allowInvalid),
                            _ => ThrowCannotLoadCertificate(),
                        };

                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSslListenerFactory(Resolve(url),
                            sslProtocols, certificateLoader, ValidateCertificate,
                            clientCertificateRequired: policy is not NoCertificatePolicy and not AllowCertificatePolicy));
                    }

                    break;
            }
        }

        return mapped;
    }

    private static Uri Resolve(string url) => new(Environment.ExpandEnvironmentVariables(url));

    [DoesNotReturn]
    private static Func<X509Certificate2> ThrowCannotLoadCertificate() => throw new InvalidOperationException(
            "Cannot load certificate from configuration. Either store information or file path should be provided.");
}