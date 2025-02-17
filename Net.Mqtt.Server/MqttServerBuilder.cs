using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using static Net.Mqtt.Server.ListenerFactoryExtensions;
using ListenerFactory = System.Func<System.Collections.Generic.IAsyncEnumerable<OOs.Net.Connections.TransportConnection>>;

namespace Net.Mqtt.Server;

/// <summary>
/// A builder for <see cref="MqttServer"/> instances.
/// </summary>
public readonly record struct MqttServerBuilder
{
    private const int DefaultTcpPort = 1883;
    private const int DefaultMaxInFlight = 32767;
    private const int DefaultMaxReceive = 32767;
    private const int DefaultMaxUnflushedBytes = 4 * 1024;
    private const int DefaultMaxPacketSize = int.MaxValue;
    private const int DefaultTopicAliasMax = 0;
    private const int DefaultTopicAliasSizeThreshold = 128;
    private const int DefaultConnectTimeoutSeconds = 5;

    private MqttProtocol Protocols { get; init; }
    private ProtocolOptions? Options { get; init; }
    private ProtocolOptions5? Options5 { get; init; }
    private TimeSpan ConnectTimeout { get; init; }
    private ILogger<MqttServer>? Logger { get; init; }
    private Dictionary<string, ListenerFactory>? ListenerFactories { get; init; }
    private IMeterFactory? MeterFactory { get; init; }
    private IMqttAuthenticationHandler? AuthenticationHandler { get; init; }

    /// <summary>
    /// Sets generic MQTT protocol options to be used for all <seealso cref="MqttServer"/> instances built by this builder.
    /// These options will define general defaults, generally common for all MQTT protocol versions.
    /// </summary>
    /// <param name="options">MQTT protocol options.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with MQTT options set to the provided one.</returns>
    public MqttServerBuilder WithOptions(ProtocolOptions options) => this with { Options = options };

    /// <summary>
    /// Sets allowed MQTT protocols to be used for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="protocols">Allowed MQTT protocols.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with allowed MQTT protocols set to the provided one.</returns>
    public MqttServerBuilder WithProtocols(MqttProtocol protocols) => this with { Protocols = protocols };

    /// <summary>
    /// Sets MQTT v3.1 as allowed protocol to be used for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with MQTT v3.1 protocol enabled.</returns>
    public MqttServerBuilder WithMQTT31() => this with { Protocols = Protocols | MqttProtocol.Level3 };

    /// <summary>
    /// Sets MQTT v3.1.1 as allowed protocol to be used for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with MQTT v3.1.1 protocol enabled.</returns>
    public MqttServerBuilder WithMQTT311() => this with { Protocols = Protocols | MqttProtocol.Level4 };

    /// <summary>
    /// Sets MQTT v5.0 as allowed protocol to be used for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with MQTT v5.0 protocol enabled.</returns>
    public MqttServerBuilder WithMQTT5() => this with { Protocols = Protocols | MqttProtocol.Level5 };

    /// <summary>
    /// Sets MQTT v5.0 as allowed protocol with custom options to be used 
    /// for all <seealso cref="MqttServer"/> instances built by this builder.
    /// These protocol options define MQTT v5.0 specific overrides, that will be applied to all MQTT5 sessions.
    /// </summary>
    /// <param name="options">MQTT v5.0 specific options.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with MQTT v5.0 protocol enabled.</returns>
    public MqttServerBuilder WithMQTT5(ProtocolOptions5 options) =>
        this with { Protocols = Protocols | MqttProtocol.Level5, Options5 = options };

    /// <summary>
    /// Sets connect timeout to be used for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="timeout">Connect timeout.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with connect timeout set to the provided one.</returns>
    public MqttServerBuilder WithConnectTimeout(TimeSpan timeout) => this with { ConnectTimeout = timeout };

    /// <summary>
    /// Sets preconfigured <seealso cref="ILogger{MqttServer}"/> instance to be used 
    /// for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="logger"><seealso cref="ILogger{MqttServer}"/> instance.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with logger set to the provided one.</returns>
    public MqttServerBuilder WithLogger(ILogger<MqttServer> logger) => this with { Logger = logger };

    /// <summary>
    /// Sets <seealso cref="IMeterFactory"/> instance to be used
    /// for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="meterFactory"><seealso cref="IMeterFactory"/> instance.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with meter factory set to the provided one.</returns>
    public MqttServerBuilder WithMeterFactory(IMeterFactory meterFactory) =>
        this with { MeterFactory = meterFactory };

    /// <summary>
    /// Sets <seealso cref="IMqttAuthenticationHandler"/> instance to be used
    /// for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="authenticationHandler"><seealso cref="IMqttAuthenticationHandler"/> instance.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with authentication handdler set to the provided one.</returns>
    public MqttServerBuilder WithAuthenticationHandler(IMqttAuthenticationHandler authenticationHandler) =>
        this with { AuthenticationHandler = authenticationHandler };

    /// <summary>
    /// Sets custom connection listeners for all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="factories">Factory callbacks that create connection listeners.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with connection listeners set to the provided one.</returns>
    public MqttServerBuilder WithListeners(params ReadOnlySpan<(string, ListenerFactory)> factories)
    {
        var dictionary = ListenerFactories ?? new(factories.Length);
        foreach (var (name, factory) in factories)
            dictionary.Add(name, factory);
        return this with { ListenerFactories = dictionary };
    }

    /// <summary>
    /// Adds TCP connection listener to all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="endPoint"><seealso cref="IPEndPoint"/> to listen on.</param>
    /// <param name="name">Endpoint name.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with TCP socket connection listener factory configured.</returns>
    public MqttServerBuilder WithTcpEndPoint(IPEndPoint endPoint, string name = "tcp") =>
        WithListeners((name, CreateTcp(endPoint)));

    /// <summary>
    /// Adds TCP connection listener to all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="address"><seealso cref="IPAddress"/> to listen on.</param>
    /// <param name="port">TCP port to listen on.</param>
    /// <param name="name">Endpoint name.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with TCP socket connection listener factory configured.</returns>
    public MqttServerBuilder WithTcpEndPoint(IPAddress? address = default, int port = DefaultTcpPort, string name = "tcp") =>
        WithListeners((name, CreateTcp(address ?? IPAddress.Any, port)));

    /// <summary>
    /// Adds TCP connection listener to all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with TCP socket connection listener factory configured.</returns>
    public MqttServerBuilder WithTcpEndPoint() => WithTcpEndPoint(name: "tcp.default");

    /// <summary>
    /// Adds TCP SSL connection listener to all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="endPoint"><seealso cref="IPEndPoint"/> to listen on.</param>
    /// <param name="certificateLoader"><seealso cref="X509Certificate2"/> loader callback.</param>
    /// <param name="validationCallback">Remote certificate validation callback.</param>
    /// <param name="clientCertificateRequired">Whether client SSL certificate is also required.</param>
    /// <param name="protocols">Enabled SSL protocols.</param>
    /// <param name="name">Endpoint name.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with TCP SSL socket connection listener factory configured.</returns>
    public MqttServerBuilder WithTcpSslEndPoint(IPEndPoint endPoint, Func<X509Certificate2> certificateLoader,
        RemoteCertificateValidationCallback? validationCallback = null, bool clientCertificateRequired = false,
        SslProtocols protocols = SslProtocols.None, string name = "tcp.ssl") =>
        WithListeners((name, CreateTcpSsl(endPoint, protocols, certificateLoader,
            validationCallback, clientCertificateRequired)));

    /// <summary>
    /// Adds WebSockets connection listener to all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="uriPrefixes">URI prefixes handled by this connection listener.</param>
    /// <param name="subProtocols">WebSocket subprotocols to be negotiated during connection.</param>
    /// <param name="name">Endpoint name.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with WebSockets connection listener factory configured.</returns>
    public MqttServerBuilder WithWebSocketsEndPoint(string[] uriPrefixes, string[]? subProtocols = null, string name = "ws") =>
        WithListeners((name, CreateWebSocket(uriPrefixes, subProtocols)));

    /// <summary>
    /// Adds WebSockets connection listener with default configuration to 
    /// all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with WebSockets connection listener factory configured.</returns>
    public MqttServerBuilder WithWebSocketsEndPoint() =>
        WithWebSocketsEndPoint(["http://+:8083/"], name: "ws.default");

    /// <summary>
    /// Adds Unix Domain Socket connection listener to all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="path">Unix Domain Socket path.</param>
    /// <param name="name">Endpoint name.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with Unix Domain Sockets connection listener factory configured.</returns>
    public MqttServerBuilder WithUnixDomainSocketEndPoint(string path, string name = "uds") =>
        WithListeners((name, CreateUnixDomainSocket(path)));

    /// <summary>
    /// Adds connection listener (described by custom <seealso cref="Uri" />) 
    /// to all <seealso cref="MqttServer"/> instances built by this builder.
    /// </summary>
    /// <param name="uri">Generic <seealso cref="Uri"/> which designates listening endpoint.</param>
    /// <param name="name">Endpoint name.</param>
    /// <returns>New <seealso cref="MqttServerBuilder"/> with connection listener factory configured by uri.</returns>
    public MqttServerBuilder WithUriEndPoint(Uri uri, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return WithListeners((name ?? uri.ToString(), Create(uri)));
    }

    /// <summary>
    /// Builds the <see cref="MqttServer"/> instance.
    /// </summary>
    /// <returns>Configured <seealso cref="MqttServer"/>.</returns>
    public MqttServer Build()
    {
        return new(Logger ?? NullLoggerFactory.Instance.CreateLogger<MqttServer>(),
            options: new()
            {
                Protocols = Protocols != default
                    ? Protocols
                    : MqttProtocol.Level4,
                ConnectTimeout = ConnectTimeout != default
                    ? ConnectTimeout :
                    TimeSpan.FromSeconds(DefaultConnectTimeoutSeconds),
                MaxInFlight = Options?.MaxInFlight ?? DefaultMaxInFlight,
                MaxReceive = Options?.MaxReceive ?? DefaultMaxReceive,
                MaxPacketSize = Options?.MaxPacketSize ?? DefaultMaxPacketSize,
                MaxUnflushedBytes = Options?.MaxUnflushedBytes ?? DefaultMaxUnflushedBytes,
                MQTT5 = new()
                {
                    MaxInFlight = Options5?.MaxInFlight ?? Options?.MaxInFlight ?? DefaultMaxInFlight,
                    MaxReceive = Options5?.MaxReceive ?? Options?.MaxReceive ?? DefaultMaxReceive,
                    MaxPacketSize = Options5?.MaxPacketSize ?? Options?.MaxPacketSize ?? DefaultMaxPacketSize,
                    MaxUnflushedBytes = Options5?.MaxUnflushedBytes ?? Options?.MaxUnflushedBytes ?? DefaultMaxUnflushedBytes,
                    TopicAliasMax = Options5?.TopicAliasMax ?? DefaultTopicAliasMax,
                    TopicAliasSizeThreshold = Options5?.TopicAliasSizeThreshold ?? DefaultTopicAliasSizeThreshold,
                }
            },
            ListenerFactories ?? new() { { "tcp.default", CreateTcp(address: IPAddress.Any, port: DefaultTcpPort) } },
            MeterFactory,
            AuthenticationHandler);
    }
}