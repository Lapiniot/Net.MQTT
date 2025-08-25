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

    public static IReadOnlyDictionary<string, Func<IAsyncEnumerable<TransportConnection>>> Map(
        this IReadOnlyDictionary<string, MqttEndpoint> endpoints, IServiceProvider serviceProvider)
    {
        var mapped = new Dictionary<string, Func<IAsyncEnumerable<TransportConnection>>>();
        var rootPath = serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;

        foreach (var (name, endpoint) in endpoints)
        {
            if (endpoint.GetFactory() is { } factory)
            {
                mapped.Add(name, factory);
                continue;
            }

            switch (endpoint)
            {
                case { EndPoint: { } ep, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.Create(ep));
                    break;

                case { EndPoint: IPEndPoint ep, UseQuic: true, Certificate: { } cert }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(ep, cert.GetLoader() ?? cert.Map(rootPath)));
                    break;

                case { EndPoint: IPEndPoint ep, Certificate: { } cert, ClientCertificateMode: var cm, SslProtocols: var sslps }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, cm);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            ep, sslps,
                            certificateLoader: cert.GetLoader() ?? cert.Map(rootPath),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;

                case { Address: var address, Port: { } port, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateTcp(address is { } ? IPAddress.Parse(address) : IPAddress.Any, port));
                    break;

                case { Address: var address, Port: { } port, UseQuic: true, Certificate: { } cert }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(
                        new IPEndPoint(address is { } ? IPAddress.Parse(address) : IPAddress.Any, port),
                        cert.GetLoader() ?? cert.Map(rootPath)));
                    break;

                case { Address: var address, Port: { } port, Certificate: { } cert, ClientCertificateMode: var cm, SslProtocols: var sslps }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, cm);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            new IPEndPoint(address is { } ? IPAddress.Parse(address) : IPAddress.Any, port), sslps,
                            certificateLoader: cert.GetLoader() ?? cert.Map(rootPath),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;

                case { Url: { } url, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.Create(url));
                    break;

                case { Url: { Scheme: "mqtt-quic" or "mqttq", Host: { } host, Port: var port }, Certificate: { } cert }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(
                                endPoint: new IPEndPoint(IPAddress.Parse(host), port),
                                certificateLoader: cert.GetLoader() ?? cert.Map(rootPath)));
                    break;

                case
                {
                    Url: { IsFile: false, Scheme: not "unix", Host: var host, Port: var port }, Certificate: { } cert,
                    ClientCertificateMode: var cmode, SslProtocols: var sslps
                }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, cmode);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            new IPEndPoint(IPAddress.Parse(host), port), sslps,
                            certificateLoader: cert.GetLoader() ?? cert.Map(rootPath),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;

                default:
                    ThrowConfigurationNotSupported(name);
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
            { Path: { } path, KeyPath: null, Password: var password } =>
#if NET9_0_OR_GREATER
                () => X509CertificateLoader.LoadPkcs12FromFile(path, password),
#else
                () => new X509Certificate2(path, password),
#endif
            { Path: { } certPemFilePath, KeyPath: { } keyPemFilePath, Password: var password } =>
                () => X509Certificate2.CreateFromPemFile(Path.Combine(rootPath, certPemFilePath), Path.Combine(rootPath, keyPemFilePath)),
            { Subject: { } subj, Store: var store, Location: var location, AllowInvalid: var allowInvalid } =>
                () => CertificateLoader.LoadFromStore(store, location, subj, allowInvalid),
            _ => ThrowCannotLoadCertificate(),
        };
    }

    [DoesNotReturn]
    private static void ThrowConfigurationNotSupported(string name) => throw new NotSupportedException(
        $"Invalid configuration options specified for endpoint '{name}'");

    [DoesNotReturn]
    private static Func<X509Certificate2> ThrowCannotLoadCertificate() => throw new InvalidOperationException(
        "Cannot load certificate from configuration. Either store information or file path should be provided.");
}