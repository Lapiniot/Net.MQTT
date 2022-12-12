using System.Net.Mqtt.Properties;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Policies;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mqtt.NetworkConnectionFactory;

namespace System.Net.Mqtt.Client;

public readonly record struct MqttClientBuilder
{
    public const int DefaultTcpPort = 1883;
    public const int DefaultSecureTcpPort = 8883;
    public MqttClientBuilder() { }

    private int Version { get; init; } = 3;
    private Func<NetworkConnection> ConnectionFactory { get; init; }
    private string ClientId { get; init; }
    private IRetryPolicy Policy { get; init; }
    private EndPoint EndPoint { get; init; }
    private IPAddress Address { get; init; }
    private string HostNameOrAddress { get; init; }
    private int Port { get; init; }
    private bool UseSsl { get; init; }
    private X509Certificate[] Certificates { get; init; }
    private string MachineName { get; init; }
    private SslProtocols EnabledSslProtocols { get; init; }
    private bool DisposeTransport { get; init; }
    private Uri WsUri { get; init; }
    private Action<ClientWebSocketOptions> ConfigureWebSocketOptions { get; init; }
    private int MaxInFlight { get; init; } = ushort.MaxValue >> 1;

    public MqttClientBuilder WithProtocol(int version) =>
        Version == version ? this : this with { Version = version is 3 or 4 ? version : ThrowVersionNotSupported() };

    public MqttClientBuilder WithProtocolV3() => Version == 3 ? this : this with { Version = 3 };

    public MqttClientBuilder WithProtocolV4() => Version == 4 ? this : this with { Version = 4 };

    public MqttClientBuilder WithClientId(string clientId) => this with { ClientId = clientId };

    public MqttClientBuilder WithMaxInFlight(int maxInFlight) => this with { MaxInFlight = maxInFlight };

    public MqttClientBuilder WithConnection(NetworkConnection connection, bool disposeTransport = false) =>
        this with
        {
            EndPoint = default,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            WsUri = default,
            ConfigureWebSocketOptions = default,
            ConnectionFactory = () => connection,
            DisposeTransport = disposeTransport
        };

    public MqttClientBuilder WithUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return uri switch
        {
            { Scheme: "tcp", Host: var host, Port: var port } => WithTcp(host, port),
            { Scheme: "tcps", Host: var host, Port: var port } => WithTcp(host, port).WithSsl(true),
            { Scheme: "unix", LocalPath: var path } => WithUnixDomain(new UnixDomainSocketEndPoint(path)),
            { Scheme: "ws" or "http" } => WithWebSockets(uri),
            { Scheme: "wss" or "https" } => WithWebSockets(uri).WithSsl(true),
            _ => ThrowSchemaNotSupported<MqttClientBuilder>()
        };
    }

    public MqttClientBuilder WithWebSockets(Uri uri, Action<ClientWebSocketOptions> configureOptions = null) =>
        this with
        {
            EndPoint = default,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            ConnectionFactory = default,
            WsUri = uri,
            ConfigureWebSocketOptions = configureOptions
        };

    public MqttClientBuilder WithTcp(IPEndPoint endPoint) =>
        this with
        {
            EndPoint = endPoint,
            Address = default,
            HostNameOrAddress = default,
            Port = default,
            WsUri = default,
            ConfigureWebSocketOptions = default,
            ConnectionFactory = default,
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
            ConfigureWebSocketOptions = default,
            ConnectionFactory = default,
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
            ConfigureWebSocketOptions = default,
            ConnectionFactory = default,
            DisposeTransport = true
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
            ConfigureWebSocketOptions = default,
            ConnectionFactory = default,
            DisposeTransport = true
        };

    public MqttClientBuilder WithSsl(bool useSsl = true) =>
        useSsl == UseSsl ? this :
        useSsl ? this with
        {
            UseSsl = true,
            ConnectionFactory = default,
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
            ConnectionFactory = default,
            DisposeTransport = true
        };

    public MqttClientBuilder WithClientCertificates(X509Certificate[] certificates) =>
        this with
        {
            UseSsl = true,
            ConnectionFactory = default,
            DisposeTransport = true,
            Certificates = certificates
        };

    public MqttClientBuilder WithReconnect(IRetryPolicy policy) => this with { Policy = policy };

    public MqttClientBuilder WithReconnect(RepeatCondition[] conditions) => this with { Policy = new ConditionalRetryPolicy(conditions) };

    public MqttClientBuilder WithReconnect(RepeatCondition condition) => this with { Policy = new ConditionalRetryPolicy(new[] { condition }) };

    private NetworkConnection BuildConnection()
    {
        return this switch
        {
            { ConnectionFactory: not null } => ConnectionFactory(),
            { EndPoint: IPEndPoint ipEP, UseSsl: true } => CreateTcpSsl(ipEP, MachineName, EnabledSslProtocols, Certificates),
            { EndPoint: IPEndPoint ipEP } => CreateTcp(ipEP),
            { EndPoint: UnixDomainSocketEndPoint udEP } => CreateUnixDomain(udEP),
            { Address: not null, UseSsl: true } => CreateTcpSsl(Address, Port > 0 ? Port : DefaultSecureTcpPort, MachineName, EnabledSslProtocols, Certificates),
            { Address: not null } => CreateTcp(Address, Port > 0 ? Port : DefaultTcpPort),
            { HostNameOrAddress: not null, UseSsl: true } => CreateTcpSsl(HostNameOrAddress, Port > 0 ? Port : DefaultSecureTcpPort, MachineName, EnabledSslProtocols, Certificates),
            { HostNameOrAddress: not null } => CreateTcp(HostNameOrAddress, Port > 0 ? Port : DefaultTcpPort),
            { WsUri: not null } => CreateWebSockets(WsUri, CreateConfigureCallback(Certificates, ConfigureWebSocketOptions)),
            _ => ThrowCannotBuildTransport()
        };
    }

    private static Action<ClientWebSocketOptions> CreateConfigureCallback(X509Certificate[] certificates, Action<ClientWebSocketOptions> configureOptions)
    {
        return (options) =>
        {
            if (certificates is not null)
            {
                options.ClientCertificates.AddRange(certificates);
            }

            configureOptions?.Invoke(options);
        };
    }

    public MqttClient Build(string clientId = null) => Version == 3 ? BuildV3(clientId) : BuildV4(clientId);

    public MqttClient3 BuildV3(string clientId = null) => new(BuildConnection(), clientId ?? ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()),
        new DefaultClientSessionStateRepository(MaxInFlight), Policy, DisposeTransport);

    public MqttClient4 BuildV4(string clientId = null) => new(BuildConnection(), clientId ?? ClientId,
        new DefaultClientSessionStateRepository(MaxInFlight), Policy, DisposeTransport);

    [DoesNotReturn]
    private static int ThrowVersionNotSupported() =>
        throw new ArgumentException(Strings.UnsupportedProtocolVersion);

    [DoesNotReturn]
    private static NetworkConnection ThrowCannotBuildTransport() =>
        throw new InvalidOperationException("Cannot build underlying network transport instance. Please, check related settings.");
}