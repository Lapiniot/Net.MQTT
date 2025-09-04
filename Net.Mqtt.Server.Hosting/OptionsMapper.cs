using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OOs.Net.Connections;

#nullable enable

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
        this IReadOnlyDictionary<string, MqttEndpoint> endpoints, Dictionary<string,
        CertificateOptions> certificates, IServiceProvider serviceProvider)
    {
        var mapped = new Dictionary<string, Func<IAsyncEnumerable<TransportConnection>>>();
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
        Func<X509Certificate2> defaultCertLoader = null!;

        foreach (var (name, endpoint) in endpoints)
        {
            switch (endpoint)
            {
                case { Factory: { } factory }:
                    mapped.Add(name, factory);
                    break;

                #region endpoint configured via EndPoint property

                case { EndPoint: IPEndPoint ep, UseQuic: true, Certificate: { } cert }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(ep, ResolveCertLoader(cert)));
                    break;

                case { EndPoint: IPEndPoint ep, UseQuic: true, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(ep, GetDefaultCertLoader()));
                    break;

                case { EndPoint: IPEndPoint ep, Certificate: { } cert, ClientCertificateMode: var cm, SslProtocols: var sslps }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, cm);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(ep, sslps, ResolveCertLoader(cert),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;

                case { EndPoint: { } ep }:
                    mapped.Add(name, ListenerFactoryExtensions.Create(ep));
                    break;

                #endregion

                #region endpoint configured via Address + Port properties

                case { Address: var address, Port: { } port, UseQuic: true, Certificate: { } cert }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(
                        new IPEndPoint(address is { } ? IPAddress.Parse(address) : IPAddress.Any, port),
                        ResolveCertLoader(cert)));
                    break;

                case { Address: var address, Port: { } port, UseQuic: true, Certificate: null }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(
                        new IPEndPoint(address is { } ? IPAddress.Parse(address) : IPAddress.Any, port),
                        GetDefaultCertLoader()));
                    break;

                case { Address: var address, Port: { } port, Certificate: { } cert, ClientCertificateMode: var cm, SslProtocols: var sslps }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, cm);
                        // This is the only case when we allow "empty" certificate config with neither Path, nor Subject, 
                        // nor Loader property specified to force switch SSL with default certificate to be used.
                        // This helps to resolve ambiguity when endpoint is configured via Address+Port and there are no
                        // other means how to infer SSL usage requirement from configuration.
                        var certificateLoader = cert is { Path: null or "", Subject: null or "", Loader: null }
                                ? GetDefaultCertLoader()
                                : ResolveCertLoader(cert);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            new IPEndPoint(address is { } ? IPAddress.Parse(address) : IPAddress.Any, port),
                            sslps, certificateLoader,
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;

                case { Address: var address, Port: { } port }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateTcp(address is { } ? IPAddress.Parse(address) : IPAddress.Any, port));
                    break;

                #endregion

                #region endpoint configured via Url property

                case { Url: { Scheme: "mqtt-quic" or "mqttq", Host: { } host, Port: var port }, Certificate: var cert }:
                    mapped.Add(name, ListenerFactoryExtensions.CreateQuic(
                        endPoint: new IPEndPoint(IPAddress.Parse(host), port),
                        certificateLoader: cert is { } ? ResolveCertLoader(cert) : GetDefaultCertLoader()));
                    break;

                case
                {
                    Url: { IsFile: false, Scheme: not "unix", Host: var host, Port: var port }, Certificate: { } cert,
                    ClientCertificateMode: var cmode, SslProtocols: var sslps
                }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, cmode);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            new IPEndPoint(IPAddress.Parse(host), port), sslps, ResolveCertLoader(cert),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;

                case
                {
                    Url: { Scheme: "tcps" or "mqtts", Host: var host, Port: var port }, Certificate: null,
                    ClientCertificateMode: var cmode, SslProtocols: var sslps
                }:
                    {
                        var policy = ResolveCertPolicy(serviceProvider, cmode);
                        mapped.Add(name, ListenerFactoryExtensions.CreateTcpSsl(
                            new IPEndPoint(IPAddress.Parse(host), port), sslps, GetDefaultCertLoader(),
                            validationCallback: (_, cert, chain, errors) => policy.Verify(cert, chain, errors),
                            clientCertificateRequired: policy.Required));
                    }

                    break;

                case { Url: { } url }:
                    mapped.Add(name, ListenerFactoryExtensions.Create(url));
                    break;

                #endregion

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

        static Func<X509Certificate2> ResolveCertLoader(CertificateOptions certificate)
        {
            return certificate switch
            {
                { Loader: { } loader } => loader,
                { Path: { Length: > 0 } path, KeyPath: null or "", Password: var password } =>
                    () => CertificateManager.LoadFromPkcs12File(path, password),
                { Path: { Length: > 0 } path, KeyPath: { Length: > 0 } keyPath, Password: var password } =>
                    () => CertificateManager.LoadFromPemFile(path, keyPath, password),
                { Subject: { Length: > 0 } subj, Store: var store, Location: var location, AllowInvalid: var allowInvalid } =>
                    () => CertificateManager.LoadFromStore(store, location, subj, allowInvalid) ?? ThrowCertificateNotFound(),
                _ => ThrowCannotLoadCertificate(),
            };
        }

        Func<X509Certificate2> GetDefaultCertLoader()
        {
            return defaultCertLoader ??= (certificates.TryGetValue("Default", out var cert) ? ResolveCertLoader(cert) : null)
                ?? (certificates.TryGetValue("Development", out cert) && cert is { Path: null, Password: { } password }
                    ? (() => CertificateManager.LoadDevCertFromAppDataPkcs12File(environment.ApplicationName, password)
                        ?? CertificateManager.LoadDevCertFromStore()
                        ?? ThrowCertificateNotFound())
                    : (Func<X509Certificate2>?)null)
                ?? (() => CertificateManager.LoadDevCertFromStore() ?? ThrowCertificateNotFound());
        }
    }

    [DoesNotReturn]
    private static void ThrowConfigurationNotSupported(string name) => throw new NotSupportedException(
        $"Invalid configuration options specified for endpoint '{name}'");

    [DoesNotReturn]
    private static Func<X509Certificate2> ThrowCannotLoadCertificate() => throw new InvalidOperationException(
        "Cannot load certificate from configuration. Either store information or file path should be provided.");

    [DoesNotReturn]
    private static X509Certificate2 ThrowCertificateNotFound() => throw new InvalidOperationException(
        "Cannot find the specified certificate.");
}