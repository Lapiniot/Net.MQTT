using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

#pragma warning disable
public class ProtocolHub5 : MqttProtocolHubWithRepository<Message5, MqttServerSessionState5, ConnectPacket, Message5>
{
    private readonly ILogger logger;
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly int maxInFlight;
    private readonly int maxUnflushedBytes;

    public ProtocolHub5(ILogger logger, IMqttAuthenticationHandler? authHandler, int maxInFlight, int maxUnflushedBytes) : base(logger)
    {
        this.logger = logger;
        this.authHandler = authHandler;
        this.maxInFlight = maxInFlight;
        this.maxUnflushedBytes = maxUnflushedBytes;
    }

    public override int ProtocolLevel => 5;
    public required IObserver<IncomingMessage5> IncomingObserver { get; init; }
    public required IObserver<SubscribeMessage5> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }

    protected override MqttServerSession CreateSession(ConnectPacket connectPacket, NetworkTransportPipe transport)
    {
        var clientId = !connectPacket.ClientId.IsEmpty
            ? UTF8.GetString(connectPacket.ClientId.Span)
            : Base32.ToBase32String(CorrelationIdGenerator.GetNext());

        return new MqttServerSession5(clientId, transport, this, logger, maxUnflushedBytes)
        {
            KeepAlive = connectPacket.KeepAlive,
            CleanStart = connectPacket.CleanStart,
            ClientTopicAliasMaximum = connectPacket.TopicAliasMaximum,
            IncomingObserver = IncomingObserver,
            SubscribeObserver = SubscribeObserver,
            UnsubscribeObserver = UnsubscribeObserver,
            ServerTopicAliasMaximum = ushort.MaxValue
        };
    }

    protected override MqttServerSessionState5 CreateState(string clientId, bool clean) =>
        new MqttServerSessionState5(clientId, DateTime.UtcNow, maxInFlight);

    protected override (Exception, ReadOnlyMemory<byte>) Validate([NotNull] ConnectPacket connPacket)
    {
        if (authHandler is not null && !authHandler.Authenticate(UTF8.GetString(connPacket.UserName.Span), UTF8.GetString(connPacket.Password.Span)))
        {
            return (new InvalidCredentialsException(), BuildConnAckPacket(ConnAckPacket.BadUserNameOrPassword));
        }

        return (null, ReadOnlyMemory<byte>.Empty);
    }

    protected static byte[] BuildConnAckPacket(byte reasonCode) => new byte[] { 0b0010_0000, 3, 0, reasonCode, 0 };

    protected sealed override void Dispatch([NotNull] MqttServerSessionState5 sessionState, Message5 message)
    {
        var (topic, payload, qos, _) = message;

        if (!sessionState.IsActive && qos == 0)
        {
            // Skip all incoming QoS 0 if session is inactive
            return;
        }

        if (!sessionState.TopicMatches(topic.Span, out var options, out var ids))
        {
            return;
        }

        var adjustedQoS = Math.Min(qos, options.QoS);

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            LogOutgoingMessage(sessionState.ClientId, UTF8.GetString(topic.Span), payload.Length, adjustedQoS, false);
        }

        sessionState.OutgoingWriter.TryWrite(qos == adjustedQoS && ids is null ? message : message with { QoSLevel = adjustedQoS, SubscriptionIds = ids });
    }
}