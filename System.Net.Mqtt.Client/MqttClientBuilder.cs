using System.Net.Mqtt.Properties;
using System.Policies;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mqtt.NetworkTransportFactory;

namespace System.Net.Mqtt.Client;

public readonly record struct MqttClientBuilder
{
    public const int DefaultTcpPort = 1883;
    public const int DefaultSecureTcpPort = 8883;

    public MqttClientBuilder()
    {
        Version = 3;
        TransportFactory = null;
        ClientId = null;
        Policy = null;
        EndPoint = null;
        Address = null;
        HostNameOrAddress = null;
        Port = 0;
        UseSsl = false;
        Certificates = null;
        MachineName = null;
        EnabledSslProtocols = default;
        DisposeTransport = false;
        WsUri = null;
        SubProtocols = null;
        KeepAliveInterval = null;
        MaxInFlight = ushort.MaxValue >> 1;
    }

    private int Version { get; init; } = 3;
    private Func<NetworkTransport> TransportFactory { get; init; }
    private string ClientId { get; init; }
    private IRetryPolicy Policy { get; init; }
    private IPEndPoint EndPoint { get; init; }
    private IPAddress Address { get; init; }
    private string HostNameOrAddress { get; init; }
    private int Port { get; init; }
    private bool UseSsl { get; init; }
    private X509Certificate[] Certificates { get; init; }
    private string MachineName { get; init; }
    private SslProtocols EnabledSslProtocols { get; init; }
    private bool DisposeTransport { get; init; }
    private Uri WsUri { get; init; }
    private string[] SubProtocols { get; init; }
    private TimeSpan? KeepAliveInterval { get; init; }
    private int MaxInFlight { get; init; }

    public MqttClientBuilder WithProtocol(int version) =>
        Version == version ? this : this with { Version = version is 3 or 4 ? version : ThrowVersionNotSupported() };
    public MqttClientBuilder WithProtocolV3() => Version == 3 ? this : this with { Version = 3 };

    public MqttClientBuilder WithProtocolV4() => Version == 4 ? this : this with { Version = 4 };

    public MqttClientBuilder WithClientId(string clientId) => this with { ClientId = clientId };

    public MqttClientBuilder WithMaxInFlight(int maxInFlight) => this with { MaxInFlight = maxInFlight };

    public MqttClientBuilder WithTransport(NetworkTransport transport, bool disposeTransport = false) =>
        this with
        {
            EndPoint = default,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            WsUri = default,
            SubProtocols = default,
            KeepAliveInterval = default,
            TransportFactory = () => transport,
            DisposeTransport = disposeTransport
        };

    public MqttClientBuilder WithUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return uri switch
        {
            { Scheme: "tcp", Host: var host, Port: var port } => WithTcp(host, port),
            { Scheme: "tcps", Host: var host, Port: var port } => WithTcp(host, port).WithSsl(true),
            { Scheme: "ws" or "http" } => WithWebSockets(uri),
            { Scheme: "wss" or "https" } => WithWebSockets(uri).WithSsl(true),
            _ => ThrowSchemaNotSupported<MqttClientBuilder>()
        };
    }

    public MqttClientBuilder WithWebSockets(Uri uri, string[] subProtocols = null, TimeSpan? keepAliveInterval = null) =>
        this with
        {
            EndPoint = default,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            TransportFactory = default,
            WsUri = uri,
            SubProtocols = subProtocols,
            KeepAliveInterval = keepAliveInterval
        };

    public MqttClientBuilder WithTcp(IPEndPoint endPoint) =>
        this with
        {
            EndPoint = endPoint,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            WsUri = default,
            SubProtocols = default,
            KeepAliveInterval = default,
            TransportFactory = default,
            DisposeTransport = true
        };

    public MqttClientBuilder WithTcp(IPAddress address, int port) =>
        this with
        {
            EndPoint = default,
            Address = address,
            HostNameOrAddress = default,
            Port = port,
            WsUri = default,
            SubProtocols = default,
            KeepAliveInterval = default,
            TransportFactory = default,
            DisposeTransport = true
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
            SubProtocols = default,
            KeepAliveInterval = default,
            TransportFactory = default,
            DisposeTransport = true
        };

    public MqttClientBuilder WithTcp(string hostNameOrAddress) => WithTcp(hostNameOrAddress, 0);

    public MqttClientBuilder WithSsl(bool useSsl = true) =>
        useSsl == UseSsl ? this :
        useSsl ? this with
        {
            UseSsl = true,
            TransportFactory = default,
            DisposeTransport = true,
            EnabledSslProtocols = SslProtocols.None
        } : this with
        {
            UseSsl = false,
            MachineName = default,
            EnabledSslProtocols = default
        };

    public MqttClientBuilder WithSsl(string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None) =>
        this with
        {
            UseSsl = true,
            MachineName = machineName,
            EnabledSslProtocols = enabledSslProtocols,
            TransportFactory = default,
            DisposeTransport = true
        };

    public MqttClientBuilder WithClientCertificates(X509Certificate[] certificates) =>
        this with
        {
            UseSsl = true,
            TransportFactory = default,
            DisposeTransport = true,
            Certificates = certificates
        };

    public MqttClientBuilder WithReconnect(IRetryPolicy policy) => this with { Policy = policy };

    public MqttClientBuilder WithReconnect(RepeatCondition[] conditions) => this with { Policy = new ConditionalRetryPolicy(conditions) };

    public MqttClientBuilder WithReconnect(RepeatCondition condition) => this with { Policy = new ConditionalRetryPolicy(new[] { condition }) };

    private NetworkTransport BuildTransport()
    {
#pragma warning disable CA2000 // False noise from buggy analyzer
        return this switch
        {
            { TransportFactory: not null } => TransportFactory(),
            { EndPoint: not null, UseSsl: true } => CreateTcpSsl(EndPoint, MachineName, EnabledSslProtocols, Certificates),
            { EndPoint: not null } => CreateTcp(EndPoint),
            { Address: not null, UseSsl: true } => CreateTcpSsl(Address, Port > 0 ? Port : DefaultSecureTcpPort, MachineName, EnabledSslProtocols, Certificates),
            { Address: not null } => CreateTcp(Address, Port > 0 ? Port : DefaultTcpPort),
            { HostNameOrAddress: not null, UseSsl: true } => CreateTcpSsl(HostNameOrAddress, Port > 0 ? Port : DefaultSecureTcpPort, MachineName, EnabledSslProtocols, Certificates),
            { HostNameOrAddress: not null } => CreateTcp(HostNameOrAddress, Port > 0 ? Port : DefaultTcpPort),
            { WsUri: not null } => CreateWebSockets(WsUri, SubProtocols, Certificates, KeepAliveInterval),
            _ => ThrowCannotBuildTransport()
        };
#pragma warning restore CA2000
    }

    public MqttClient Build(string clientId = null) => Version == 3 ? BuildV3(clientId) : BuildV4(clientId);

    public MqttClient3 BuildV3(string clientId = null) => new(BuildTransport(), clientId ?? ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()),
        new DefaultClientSessionStateRepository(MaxInFlight), Policy, DisposeTransport);

    public MqttClient4 BuildV4(string clientId = null) => new(BuildTransport(), clientId ?? ClientId,
        new DefaultClientSessionStateRepository(MaxInFlight), Policy, DisposeTransport);

    [DoesNotReturn]
    private static int ThrowVersionNotSupported() =>
        throw new ArgumentException(Strings.UnsupportedProtocolVersion);

    [DoesNotReturn]
    private static NetworkTransport ThrowCannotBuildTransport() =>
        throw new InvalidOperationException("Cannot build underlaying network transport instance. Please, check related settings.");
}