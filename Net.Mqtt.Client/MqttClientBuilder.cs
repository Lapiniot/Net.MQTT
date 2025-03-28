using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Net.Mqtt.Properties;
using static OOs.Net.Connections.ThrowHelper;

namespace Net.Mqtt.Client;

public readonly record struct MqttClientBuilder
{
    public const int DefaultTcpPort = 1883;
    public const int DefaultSecureTcpPort = 8883;
    public const int DefaultQuicPort = 8885;
    public const string DefaultSslProtocolName = "mqtt-quic";

    public int Version { get; private init; } = 3;
    private string? ClientId { get; init; }
    private EndPoint? EndPoint { get; init; }
    private bool UseSsl { get; init; }
    private bool UseQuic { get; init; }
    private X509Certificate[]? Certificates { get; init; }
    private SslProtocols EnabledSslProtocols { get; init; }
    private Action<ClientWebSocketOptions>? WsConfigureOptions { get; init; }
    private HttpMessageInvoker? WsMessageInvoker { get; init; }
    private int MaxInFlight { get; init; } = ushort.MaxValue >>> 1;

    public MqttClientBuilder() { }

    public MqttClientBuilder WithProtocol(int version) => Version != version
        ? this with { Version = version is 3 or 4 or 5 ? version : ThrowVersionNotSupported<int>() }
        : this;

    public MqttClientBuilder WithProtocolV3() => Version == 3 ? this : this with { Version = 3 };

    public MqttClientBuilder WithProtocolV4() => Version == 4 ? this : this with { Version = 4 };

    public MqttClientBuilder WithProtocolV5() => Version == 5 ? this : this with { Version = 5 };

    public MqttClientBuilder WithClientId(string clientId) => this with { ClientId = clientId };

    public MqttClientBuilder WithMaxInFlight(int maxInFlight) => this with { MaxInFlight = maxInFlight };

    public MqttClientBuilder WithUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return uri switch
        {
            { Scheme: "tcp" or "mqtt", Host: var host, Port: var port } =>
                WithTcp(host, port > 0 ? port : DefaultTcpPort),
            { Scheme: "tcps" or "mqtts", Host: var host, Port: var port } =>
                WithTcpSsl(host, port > 0 ? port : DefaultSecureTcpPort),
            { Scheme: "mqtt-quic" or "mqttq", Host: var host, Port: var port } =>
                WithQuic(host, port > 0 ? port : DefaultQuicPort),
            { Scheme: "unix" } or { IsFile: true } =>
                WithUnixDomain(SocketBuilderExtensions.ResolveUnixDomainSocketPath(uri.LocalPath)),
            { Scheme: "ws" or "http" or "wss" or "https" } =>
                WithWebSockets(uri),
            _ => ThrowSchemaNotSupported<MqttClientBuilder>()
        };
    }

    #region HTTP WebSockets

    public MqttClientBuilder WithWebSockets(Uri uri) => this with
    {
        EndPoint = new UriEndPoint(uri),
        UseQuic = false,
        UseSsl = false,
        EnabledSslProtocols = default
    };

    public MqttClientBuilder WithWebSocketOptions(Action<ClientWebSocketOptions>? configureOptions) => this with
    {
        WsConfigureOptions = configureOptions
    };

    public MqttClientBuilder WithWebSocketHttpMessageInvoker(HttpMessageInvoker invoker) => this with
    {
        WsMessageInvoker = invoker
    };

    #endregion

    #region TCP sockets

    private MqttClientBuilder WithTcpEndpoint(EndPoint endPoint) => this with
    {
        EndPoint = endPoint,
        UseQuic = false,
        UseSsl = false,
        EnabledSslProtocols = default,
        WsConfigureOptions = default,
        WsMessageInvoker = default
    };

    public MqttClientBuilder WithTcp(IPEndPoint endPoint) =>
        WithTcpEndpoint(endPoint);

    public MqttClientBuilder WithTcp(IPAddress address, int port = DefaultTcpPort) =>
        WithTcpEndpoint(new IPEndPoint(address, port));

    public MqttClientBuilder WithTcp(string hostNameOrAddress, int port = DefaultTcpPort,
        AddressFamily addressFamily = AddressFamily.Unspecified) =>
        WithTcpEndpoint(CreateEndpoint(hostNameOrAddress, port, addressFamily));

    #endregion

    #region TCP SSL sockets

    private MqttClientBuilder WithTcpSslEndpoint(EndPoint endPoint, SslProtocols enabledSslProtocols) => this with
    {
        EndPoint = endPoint,
        UseQuic = false,
        UseSsl = true,
        EnabledSslProtocols = enabledSslProtocols,
        WsConfigureOptions = default,
        WsMessageInvoker = default
    };

    public MqttClientBuilder WithTcpSsl(IPEndPoint endPoint, SslProtocols enabledSslProtocols = SslProtocols.None) =>
        WithTcpSslEndpoint(endPoint, enabledSslProtocols);

    public MqttClientBuilder WithTcpSsl(IPAddress address, int port = DefaultSecureTcpPort,
        SslProtocols enabledSslProtocols = SslProtocols.None) =>
        WithTcpSslEndpoint(new IPEndPoint(address, port), enabledSslProtocols);

    public MqttClientBuilder WithTcpSsl(string hostNameOrAddress, int port = DefaultSecureTcpPort,
        AddressFamily addressFamily = AddressFamily.Unspecified, SslProtocols enabledSslProtocols = SslProtocols.None) =>
        WithTcpSslEndpoint(CreateEndpoint(hostNameOrAddress, port, addressFamily), enabledSslProtocols);

    public MqttClientBuilder WithClientCertificates(X509Certificate[] certificates) => this with
    {
        UseSsl = true,
        Certificates = certificates
    };

    #endregion

    #region QUIC protocol

    private MqttClientBuilder WithQuicEndpoint(EndPoint endPoint) => this with
    {
        EndPoint = endPoint,
        UseQuic = true,
        UseSsl = false,
        EnabledSslProtocols = default,
        WsConfigureOptions = default,
        WsMessageInvoker = default
    };

    public MqttClientBuilder WithQuic(IPEndPoint endPoint) =>
        WithQuicEndpoint(endPoint);

    public MqttClientBuilder WithQuic(IPAddress address, int port = DefaultQuicPort) =>
        WithQuicEndpoint(new IPEndPoint(address, port));

    public MqttClientBuilder WithQuic(string hostNameOrAddress, int port = DefaultQuicPort,
        AddressFamily addressFamily = AddressFamily.Unspecified) =>
        WithQuicEndpoint(CreateEndpoint(hostNameOrAddress, port, addressFamily));

    #endregion

    #region Unix Domain Sockets

    public MqttClientBuilder WithUnixDomain(UnixDomainSocketEndPoint endPoint) => this with
    {
        EndPoint = endPoint,
        UseQuic = false,
        UseSsl = false,
        EnabledSslProtocols = default,
        WsConfigureOptions = default,
        WsMessageInvoker = default
    };

    #endregion

    private TransportConnection BuildConnection()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        return this switch
        {
            { EndPoint: IPEndPoint ipEndPoint, UseSsl: true } =>
                ClientTcpSslSocketTransportConnection.Create(ipEndPoint, null, EnabledSslProtocols, Certificates),
            { EndPoint: IPEndPoint ipEndPoint, UseQuic: true } => QuicTransportConnection.IsSupported
                ? ClientQuicTransportConnection.Create(ipEndPoint, new(DefaultSslProtocolName))
                : ThrowQuicNotSupported<TransportConnection>(),
            { EndPoint: IPEndPoint ipEndPoint } =>
                ClientTcpSocketTransportConnection.Create(ipEndPoint),
            { EndPoint: UnixDomainSocketEndPoint unixDomainEndPoint } =>
                ClientUnixDomainSocketTransportConnection.Create(unixDomainEndPoint),
            { EndPoint: DnsEndPoint dnsEndPoint, UseSsl: true } =>
                ClientTcpSslSocketTransportConnection.Create(dnsEndPoint, null, EnabledSslProtocols, Certificates),
            { EndPoint: DnsEndPoint dnsEndPoint, UseQuic: true } => QuicTransportConnection.IsSupported
                ? ClientQuicTransportConnection.Create(dnsEndPoint, new(DefaultSslProtocolName))
                : ThrowQuicNotSupported<TransportConnection>(),
            { EndPoint: DnsEndPoint dnsEndPoint } =>
                ClientTcpSocketTransportConnection.Create(dnsEndPoint),
            { EndPoint: UriEndPoint { Uri: { } uri } } =>
                ClientWebSocketTransportConnection.Create(MakeValidWsUri(uri),
                    CreateConfigureCallback(Certificates, WsConfigureOptions), WsMessageInvoker),
            _ => ThrowCannotBuildTransport()
        };
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    private static EndPoint CreateEndpoint(string hostNameOrAddress, int port, AddressFamily addressFamily)
    {
        return addressFamily is AddressFamily.Unspecified
            ? IPAddress.TryParse(hostNameOrAddress, out var address)
                ? new IPEndPoint(address, port)
                : new DnsEndPoint(hostNameOrAddress, port, AddressFamily.InterNetwork)
            : new DnsEndPoint(hostNameOrAddress, port, addressFamily);
    }

    private static Uri MakeValidWsUri(Uri uri) => uri switch
    {
        { Scheme: "ws" or "wss" } => uri,
        { Scheme: "http" } => new UriBuilder(uri) { Scheme = "ws" }.Uri,
        { Scheme: "https" } => new UriBuilder(uri) { Scheme = "wss" }.Uri,
        _ => ThrowSchemaNotSupported<Uri>()
    };

    private static Action<ClientWebSocketOptions> CreateConfigureCallback(X509Certificate[]? certificates,
        Action<ClientWebSocketOptions>? configureOptions)
    {
        return (options) =>
        {
            options.AddSubProtocol("mqttv3.1");
            options.AddSubProtocol("mqtt");

            if (certificates is not null)
                options.ClientCertificates.AddRange(certificates);

            configureOptions?.Invoke(options);
        };
    }

    public MqttClient Build(string? clientId = null) => Version switch
    {
        3 => BuildV3(clientId),
        4 => BuildV4(clientId),
        5 => BuildV5(clientId),
        _ => ThrowVersionNotSupported<MqttClient>(),
    };

    public MqttClient3 BuildV3(string? clientId = null) =>
        new(BuildConnection(), true, clientId ?? ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), MaxInFlight);

    public MqttClient4 BuildV4(string? clientId = null) =>
        new(BuildConnection(), true, clientId ?? ClientId, MaxInFlight);

    public MqttClient5 BuildV5(string? clientId = null) =>
        new(BuildConnection(), true, clientId ?? ClientId, MaxInFlight);

    [DoesNotReturn]
    private static T ThrowVersionNotSupported<T>() =>
        throw new ArgumentException(Strings.UnsupportedProtocolVersion);

    [DoesNotReturn]
    private static TransportConnection ThrowCannotBuildTransport() =>
        throw new InvalidOperationException("Cannot build underlying network transport instance. Please, check related settings.");

    [DoesNotReturn]
    private static T ThrowSchemaNotSupported<T>() =>
        throw new ArgumentException("Uri schema is not supported.");
}