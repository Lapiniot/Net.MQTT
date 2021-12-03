using System.Net.Mqtt.Client.Properties;
using System.Policies;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mqtt.NetworkTransportFactory;

namespace System.Net.Mqtt.Client;

#pragma warning disable CA1805
public readonly record struct MqttClientBuilder()
{
    private int Version { get; init; } = 3;
    private Func<NetworkTransport> TransportFactory { get; init; } = default;
    private ClientSessionStateRepository Repository { get; init; } = default;
    private string ClientId { get; init; } = default;
    private IRetryPolicy Policy { get; init; } = default;
    private IPEndPoint EndPoint { get; init; } = default;
    private IPAddress Address { get; init; } = default;
    private string HostNameOrAddress { get; init; } = default;
    private int Port { get; init; } = default;
    private bool UseSsl { get; init; } = default;
    private X509Certificate2[] Certificates { get; init; } = default;
    private string MachineName { get; init; } = default;
    private SslProtocols EnabledSslProtocols { get; init; } = default;
    private bool DisposeTransport { get; init; } = default;
    private Uri WsUri { get; init; } = default;
    private string[] SubProtocols { get; init; } = default;
    private TimeSpan? KeepAliveInterval { get; init; } = default;

    public readonly MqttClientBuilder WithProtocolV3()
    {
        return this with { Version = 3 };
    }

    public readonly MqttClientBuilder WithProtocolV4()
    {
        return this with { Version = 4 };
    }

    public readonly MqttClientBuilder WithClientId(string clientId)
    {
        return this with { ClientId = clientId };
    }

    public readonly MqttClientBuilder WithSessionStateRepository(ClientSessionStateRepository repository)
    {
        return this with { Repository = repository };
    }

    public readonly MqttClientBuilder WithTransport(NetworkTransport transport, bool disposeTransport = false)
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

    public readonly MqttClientBuilder WithUri(Uri uri)
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
            TransportFactory = () => Create(uri),
            DisposeTransport = true
        };
    }

    public readonly MqttClientBuilder WithWebSockets(Uri uri, string[] subProtocols = null, TimeSpan? keepAliveInterval = null)
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

    public readonly MqttClientBuilder WithTcp(IPEndPoint endPoint)
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

    public readonly MqttClientBuilder WithTcp(IPAddress address, int port)
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

    public readonly MqttClientBuilder WithTcp(string hostNameOrAddress, int port)
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

    public readonly MqttClientBuilder WithSSL(string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None)
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

    public readonly MqttClientBuilder WithClientCertificates(X509Certificate2[] certificates)
    {
        return this with
        {
            UseSsl = true,
            TransportFactory = default,
            DisposeTransport = true,
            Certificates = certificates
        };
    }

    public readonly MqttClientBuilder WithReconnect(IRetryPolicy policy)
    {
        return this with { Policy = policy };
    }

    public readonly MqttClientBuilder WithReconnect(RepeatCondition[] conditions)
    {
        return this with { Policy = new ConditionalRetryPolicy(conditions) };
    }

    public readonly MqttClientBuilder WithReconnect(RepeatCondition condition)
    {
        return this with { Policy = new ConditionalRetryPolicy(new[] { condition }) };
    }

    private readonly NetworkTransport BuildTransport()
    {
#pragma warning disable CA2000 // False noise from buggy analyzer
        return this switch
        {
            { TransportFactory: not null } => TransportFactory(),
            { EndPoint: not null, UseSsl: true } => CreateTcpSsl(EndPoint, MachineName, EnabledSslProtocols, Certificates),
            { EndPoint: not null } => CreateTcp(EndPoint),
            { Address: not null, Port: > 0, UseSsl: true } => CreateTcpSsl(Address, Port, MachineName, EnabledSslProtocols, Certificates),
            { Address: not null, Port: > 0 } => CreateTcp(Address, Port),
            { HostNameOrAddress: not null, Port: > 0, UseSsl: true } => CreateTcpSsl(HostNameOrAddress, Port, MachineName, EnabledSslProtocols, Certificates),
            { HostNameOrAddress: not null, Port: > 0 } => CreateTcp(HostNameOrAddress, Port),
            { WsUri: not null } => CreateWebSockets(WsUri, SubProtocols, Certificates, KeepAliveInterval),
            _ => throw new InvalidOperationException(Strings.CannotBuildTransport),
        };
#pragma warning restore CA2000
    }

    public readonly MqttClient Build()
    {
        return Version == 3
            ? new MqttClient3(BuildTransport(), ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), Repository, Policy, DisposeTransport)
            : new MqttClient4(BuildTransport(), ClientId, Repository, Policy, DisposeTransport);
    }

    public readonly MqttClient3 BuildV3()
    {
        return new MqttClient3(BuildTransport(), ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), Repository, Policy, DisposeTransport);
    }

    public readonly MqttClient4 BuildV4()
    {
        return new MqttClient4(BuildTransport(), ClientId, Repository, Policy, DisposeTransport);
    }
}