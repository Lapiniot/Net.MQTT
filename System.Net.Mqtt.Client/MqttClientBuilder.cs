using System.Policies;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mqtt.NetworkTransportFactory;
using static System.Net.Mqtt.Client.Properties.Strings;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Client;

public readonly record struct MqttClientBuilder
{
    public const int DefaultTcpPort = 1883;
    public const int DefaultSecureTcpPort = 8883;

    private int Version { get; init; } = 3;
    private Func<NetworkTransport> TransportFactory { get; init; }
    private ClientSessionStateRepository Repository { get; init; }
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

    public MqttClientBuilder WithProtocol(int version)
    {
        return Version == version ? this : this with { Version = version is 3 or 4 ? version : throw new ArgumentException(UnsupportedProtocolVersion) };
    }

    public MqttClientBuilder WithProtocolV3()
    {
        return Version == 3 ? this : this with { Version = 3 };
    }

    public MqttClientBuilder WithProtocolV4()
    {
        return Version == 4 ? this : this with { Version = 4 };
    }

    public MqttClientBuilder WithClientId(string clientId)
    {
        return this with { ClientId = clientId };
    }

    public MqttClientBuilder WithSessionStateRepository(ClientSessionStateRepository repository)
    {
        return this with { Repository = repository };
    }

    public MqttClientBuilder WithTransport(NetworkTransport transport, bool disposeTransport = false)
    {
        return this with
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
    }

    public MqttClientBuilder WithUri(Uri uri)
    {
        return uri switch
        {
            null => throw new ArgumentNullException(nameof(uri)),
            { Scheme: "tcp", Host: var host, Port: var port } => WithTcp(host, port),
            { Scheme: "tcps", Host: var host, Port: var port } => WithTcp(host, port).WithSsl(true),
            { Scheme: "ws" or "http" } => WithWebSockets(uri),
            { Scheme: "wss" or "https" } => WithWebSockets(uri).WithSsl(true),
            _ => throw new NotImplementedException(SchemaNotSupported)
        };
    }

    public MqttClientBuilder WithWebSockets(Uri uri, string[] subProtocols = null, TimeSpan? keepAliveInterval = null)
    {
        return this with
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
    }

    public MqttClientBuilder WithTcp(IPEndPoint endPoint)
    {
        return this with
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
    }

    public MqttClientBuilder WithTcp(IPAddress address, int port)
    {
        return this with
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
    }

    public MqttClientBuilder WithTcp(IPAddress address)
    {
        return WithTcp(address, 0);
    }

    public MqttClientBuilder WithTcp(string hostNameOrAddress, int port)
    {
        return this with
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
    }

    public MqttClientBuilder WithTcp(string hostNameOrAddress)
    {
        return WithTcp(hostNameOrAddress, 0);
    }

    public MqttClientBuilder WithSsl(bool useSsl = true)
    {
        return useSsl == UseSsl ? this : useSsl ? this with
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
    }

    public MqttClientBuilder WithSsl(string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None)
    {
        return this with
        {
            UseSsl = true,
            MachineName = machineName,
            EnabledSslProtocols = enabledSslProtocols,
            TransportFactory = default,
            DisposeTransport = true
        };
    }

    public MqttClientBuilder WithClientCertificates(X509Certificate[] certificates)
    {
        return this with
        {
            UseSsl = true,
            TransportFactory = default,
            DisposeTransport = true,
            Certificates = certificates
        };
    }

    public MqttClientBuilder WithReconnect(IRetryPolicy policy)
    {
        return this with { Policy = policy };
    }

    public MqttClientBuilder WithReconnect(RepeatCondition[] conditions)
    {
        return this with { Policy = new ConditionalRetryPolicy(conditions) };
    }

    public MqttClientBuilder WithReconnect(RepeatCondition condition)
    {
        return this with { Policy = new ConditionalRetryPolicy(new[] { condition }) };
    }

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
            _ => throw new InvalidOperationException(CannotBuildTransport)
        };
#pragma warning restore CA2000
    }

    public MqttClient Build()
    {
        return Version == 3
            ? new MqttClient3(BuildTransport(), ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), Repository, Policy, DisposeTransport)
            : new MqttClient4(BuildTransport(), ClientId, Repository, Policy, DisposeTransport);
    }

    public MqttClient3 BuildV3()
    {
        return new MqttClient3(BuildTransport(), ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), Repository, Policy, DisposeTransport);
    }

    public MqttClient4 BuildV4()
    {
        return new MqttClient4(BuildTransport(), ClientId, Repository, Policy, DisposeTransport);
    }
}