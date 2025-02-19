using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Net.Mqtt.Properties;

namespace Net.Mqtt.Client;

public readonly record struct MqttClientBuilder
{
    public const int DefaultTcpPort = 1883;
    public const int DefaultSecureTcpPort = 8883;

    public MqttClientBuilder() { }

    public int Version { get; private init; } = 3;
    private Func<TransportConnection>? ConnectionFactory { get; init; }
    private string? ClientId { get; init; }
    private EndPoint? EndPoint { get; init; }
    private IPAddress? Address { get; init; }
    private string? HostNameOrAddress { get; init; }
    private int Port { get; init; }
    private bool UseSsl { get; init; }
    private X509Certificate[]? Certificates { get; init; }
    private string? MachineName { get; init; }
    private SslProtocols EnabledSslProtocols { get; init; }
    private bool DisposeConnection { get; init; }
    private Uri? WsUri { get; init; }
    private Action<ClientWebSocketOptions>? WsConfigureOptions { get; init; }
    private HttpMessageInvoker? WsMessageInvoker { get; init; }
    private int MaxInFlight { get; init; } = ushort.MaxValue >>> 1;

    public MqttClientBuilder WithProtocol(int version) =>
        Version == version ? this : this with { Version = version is 3 or 4 or 5 ? version : ThrowVersionNotSupported() };

    public MqttClientBuilder WithProtocolV3() => Version == 3 ? this : this with { Version = 3 };

    public MqttClientBuilder WithProtocolV4() => Version == 4 ? this : this with { Version = 4 };

    public MqttClientBuilder WithProtocolV5() => Version == 5 ? this : this with { Version = 5 };

    public MqttClientBuilder WithClientId(string clientId) => this with { ClientId = clientId };

    public MqttClientBuilder WithMaxInFlight(int maxInFlight) => this with { MaxInFlight = maxInFlight };

    public MqttClientBuilder WithConnection(TransportConnection connection, bool disposeConnection = false) =>
        this with
        {
            EndPoint = null,
            Address = null,
            HostNameOrAddress = null,
            Port = 0,
            WsUri = null,
            WsConfigureOptions = null,
            ConnectionFactory = () => connection,
            DisposeConnection = disposeConnection
        };

    public MqttClientBuilder WithUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return uri switch
        {
            { Scheme: "tcp" or "mqtt", Host: var host, Port: var port } =>
                WithTcp(host, port > 0 ? port : DefaultTcpPort).WithSsl(false),
            { Scheme: "tcps" or "mqtts", Host: var host, Port: var port } =>
                WithTcp(host, port > 0 ? port : DefaultSecureTcpPort).WithSsl(true),
            { Scheme: "unix" } or { IsFile: true } =>
                WithUnixDomain(SocketBuilderExtensions.ResolveUnixDomainSocketPath(uri.LocalPath)),
            { Scheme: "ws" or "http" } =>
                WithWebSockets(uri, WsConfigureOptions).WithSsl(false),
            { Scheme: "wss" or "https" } =>
                WithWebSockets(uri, WsConfigureOptions).WithSsl(true),
            _ => ThrowSchemaNotSupported<MqttClientBuilder>()
        };
    }

    public MqttClientBuilder WithWebSockets(Uri uri, Action<ClientWebSocketOptions>? configureOptions = null) =>
        this with
        {
            EndPoint = default,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            ConnectionFactory = default,
            WsUri = uri,
            WsConfigureOptions = configureOptions
        };

    public MqttClientBuilder WithWebSocketOptions(Action<ClientWebSocketOptions>? configureOptions) =>
        this with { WsConfigureOptions = configureOptions };

    public MqttClientBuilder WithWebSocketHttpMessageInvoker(HttpMessageInvoker invoker) =>
        this with { WsMessageInvoker = invoker };

    public MqttClientBuilder WithTcp(IPEndPoint endPoint) =>
        this with
        {
            EndPoint = endPoint,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            WsUri = default,
            WsConfigureOptions = default,
            ConnectionFactory = default,
            DisposeConnection = true
        };

    public MqttClientBuilder WithTcp(IPAddress address, int port) =>
        this with
        {
            EndPoint = default,
            Address = address,
            HostNameOrAddress = default,
            Port = port,
            WsUri = default,
            WsConfigureOptions = default,
            ConnectionFactory = default,
            DisposeConnection = true
        };

    public MqttClientBuilder WithTcp(IPAddress address) => WithTcp(address, 0);

    public MqttClientBuilder WithTcp(string hostNameOrAddress, int port) =>
        this with
        {
            EndPoint = default,
            Address = default,
            HostNameOrAddress = hostNameOrAddress,
            Port = port,
            WsUri = default,
            WsConfigureOptions = default,
            ConnectionFactory = default,
            DisposeConnection = true
        };

    public MqttClientBuilder WithTcp(string hostNameOrAddress) => WithTcp(hostNameOrAddress, 0);

    public MqttClientBuilder WithUnixDomain(UnixDomainSocketEndPoint endPoint) =>
        this with
        {
            EndPoint = endPoint,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            WsUri = default,
            WsConfigureOptions = default,
            ConnectionFactory = default,
            DisposeConnection = true
        };

    public MqttClientBuilder WithSsl(bool useSsl = true) =>
        useSsl == UseSsl ? this :
        useSsl ? this with
        {
            UseSsl = true,
            ConnectionFactory = default,
            DisposeConnection = true,
            EnabledSslProtocols = SslProtocols.None
        } : this with
        {
            UseSsl = false,
            MachineName = default,
            EnabledSslProtocols = default
        };

    public MqttClientBuilder WithSsl(string? machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None) =>
        this with
        {
            UseSsl = true,
            MachineName = machineName,
            EnabledSslProtocols = enabledSslProtocols,
            ConnectionFactory = default,
            DisposeConnection = true
        };

    public MqttClientBuilder WithClientCertificates(X509Certificate[] certificates) =>
        this with
        {
            UseSsl = true,
            ConnectionFactory = default,
            DisposeConnection = true,
            Certificates = certificates
        };

    private TransportConnection BuildConnection()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        return this switch
        {
            { ConnectionFactory: not null } => ConnectionFactory(),
            { EndPoint: IPEndPoint ipEndPoint, UseSsl: true } =>
                ClientTcpSslSocketTransportConnection.Create(ipEndPoint, MachineName, EnabledSslProtocols, Certificates),
            { EndPoint: IPEndPoint ipEndPoint } =>
                ClientTcpSocketTransportConnection.Create(ipEndPoint),
            { EndPoint: UnixDomainSocketEndPoint unixDomainEndPoint } =>
                ClientUnixDomainSocketTransportConnection.Create(unixDomainEndPoint),
            { Address: { } address, UseSsl: true } =>
                ClientTcpSslSocketTransportConnection.Create(new(address, Port > 0 ? Port : DefaultSecureTcpPort),
                    MachineName, EnabledSslProtocols, Certificates),
            { Address: { } address } =>
                ClientTcpSocketTransportConnection.Create(new(address, Port > 0 ? Port : DefaultTcpPort)),
            { HostNameOrAddress: { } host, UseSsl: true } =>
                ClientTcpSslSocketTransportConnection.Create(host, Port > 0 ? Port : DefaultSecureTcpPort,
                    machineName: MachineName, enabledSslProtocols: EnabledSslProtocols, clientCertificates: Certificates),
            { HostNameOrAddress: { } host } =>
                ClientTcpSocketTransportConnection.Create(host, Port > 0 ? Port : DefaultTcpPort),
            { WsUri: { } uri } =>
                ClientWebSocketTransportConnection.Create(MakeValidWsUri(uri),
                    CreateConfigureCallback(Certificates, WsConfigureOptions), WsMessageInvoker),
            _ => ThrowCannotBuildTransport()
        };
#pragma warning restore CA2000 // Dispose objects before losing scope

    }

    private static Uri MakeValidWsUri(Uri uri)
    {
        return uri switch
        {
            { Scheme: "ws" or "wss" } => uri,
            { Scheme: "http" } => new UriBuilder(uri) { Scheme = "ws" }.Uri,
            { Scheme: "https" } => new UriBuilder(uri) { Scheme = "wss" }.Uri,
            _ => ThrowSchemaNotSupported<Uri>()
        };
    }

    private static Action<ClientWebSocketOptions> CreateConfigureCallback(X509Certificate[]? certificates, Action<ClientWebSocketOptions>? configureOptions)
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

    public MqttClient Build(string? clientId = null)
    {
        switch (Version)
        {
            case 3: return BuildV3(clientId);
            case 4: return BuildV4(clientId);
            case 5: return BuildV5(clientId);
            default: ThrowVersionNotSupported(); return null;
        }
    }

    public MqttClient3 BuildV3(string? clientId = null) =>
        new(BuildConnection(), DisposeConnection, clientId ?? ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), MaxInFlight);

    public MqttClient4 BuildV4(string? clientId = null) =>
        new(BuildConnection(), DisposeConnection, clientId ?? ClientId, MaxInFlight);

    public MqttClient5 BuildV5(string? clientId = null) =>
        new(BuildConnection(), DisposeConnection, clientId ?? ClientId, MaxInFlight);

    [DoesNotReturn]
    private static int ThrowVersionNotSupported() =>
        throw new ArgumentException(Strings.UnsupportedProtocolVersion);

    [DoesNotReturn]
    private static TransportConnection ThrowCannotBuildTransport() =>
        throw new InvalidOperationException("Cannot build underlying network transport instance. Please, check related settings.");

    [DoesNotReturn]
    private static T ThrowSchemaNotSupported<T>() =>
        throw new ArgumentException("Uri schema is not supported.");
}