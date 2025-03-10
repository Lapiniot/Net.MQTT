using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Server.Protocol.V5;

public class ProtocolHub5(ILogger logger, IMqttAuthenticationHandler? authHandler, ProtocolOptions5 options) :
    MqttProtocolHubWithRepository<Message5, MqttServerSessionState5, ConnectPacket, Message5>(logger)
{
    private readonly ILogger logger = logger;
    private readonly IMqttAuthenticationHandler? authHandler = authHandler;

    public override int ProtocolLevel => 5;
    public required IObserver<IncomingMessage5> IncomingObserver { get; init; }
    public required IObserver<SubscribeMessage5> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }

    protected override MqttServerSession CreateSession([NotNull] ConnectPacket connectPacket, TransportConnection connection)
    {
        var (clientId, assigned) = !connectPacket.ClientId.IsEmpty
            ? (UTF8.GetString(connectPacket.ClientId.Span), false)
            : (Base32.ToBase32String(CorrelationIdGenerator.GetNext()), true);

        return new MqttServerSession5(clientId, connection, this, logger, options.MaxUnflushedBytes,
            Math.Min(options.MaxInFlight, connectPacket.ReceiveMaximum), options.MaxPacketSize)
        {
            KeepAlive = connectPacket.KeepAlive,
            CleanStart = connectPacket.CleanStart,
            ClientTopicAliasMaximum = connectPacket.TopicAliasMaximum,
            IncomingObserver = IncomingObserver,
            SubscribeObserver = SubscribeObserver,
            UnsubscribeObserver = UnsubscribeObserver,
            PacketRxObserver = PacketRxObserver,
            PacketTxObserver = PacketTxObserver,
            TopicAliasMaximum = options.TopicAliasMax,
            ReceiveMaximum = options.MaxReceive,
            ExpiryInterval = connectPacket.SessionExpiryInterval,
            WillMessage = !connectPacket.WillTopic.IsEmpty ? new(connectPacket.WillTopic, connectPacket.WillPayload, connectPacket.WillQoS, connectPacket.WillRetain)
            {
                ContentType = connectPacket.WillContentType,
                PayloadFormat = connectPacket.WillPayloadFormat,
                UserProperties = connectPacket.WillUserProperties,
                ResponseTopic = connectPacket.WillResponseTopic,
                CorrelationData = connectPacket.WillCorrelationData,
                ExpiresAt = connectPacket.WillExpiryInterval is { } interval ? DateTime.UtcNow.AddSeconds(interval).Ticks : null
            } : null,
            WillDelayInterval = connectPacket.WillDelayInterval,
            HasAssignedClientId = assigned,
            MaxSendPacketSize = (int)connectPacket.MaximumPacketSize.GetValueOrDefault(int.MaxValue),
            TopicAliasSizeThreshold = options.TopicAliasSizeThreshold
        };
    }

    protected override MqttServerSessionState5 CreateState(string clientId) => new(clientId, DateTime.UtcNow);

    protected override (Exception?, ReadOnlyMemory<byte>) Validate([NotNull] ConnectPacket connPacket)
    {
        return authHandler is not null && !authHandler.Authenticate(UTF8.GetString(connPacket.UserName.Span), UTF8.GetString(connPacket.Password.Span))
            ? (new InvalidCredentialsException(), BuildConnAckPacket(ConnAckPacket.BadUserNameOrPassword))
            : (null, ReadOnlyMemory<byte>.Empty);
    }

    protected static byte[] BuildConnAckPacket(byte reasonCode) => [0b0010_0000, 3, 0, reasonCode, 0];

    protected sealed override void Dispatch([NotNull] MqttServerSessionState5 sessionState, (MqttSessionState Sender, Message5 Message) message)
    {
        var (sender, m) = message;
        var qos = (int)m.QoSLevel;
        if (qos == 0 && !sessionState.IsActive
            || !sessionState.TopicMatches(m.Topic.Span, out var options, out var ids)
            || options.NoLocal && MqttSessionState.SessionEquals(sessionState, sender))
        {
            return;
        }

        var retain = m.Retain;
        var actualQoS = Math.Min(qos, options.QoS);
        var actualRetain = options.RetainAsPublished && retain;

        // Try to avoid excessive message cloning via retransmission of the original message as soon as
        // calculated QoS, Retain and SubscriptionIds are the same as in the original message.
        // This can dramatically decrease allocation costs when subscription's RetainAsPublished=true
        // or max allowed QoS is higher then published messages have and SubscriptionIds are not used e.g.
        if (actualQoS != qos || actualRetain != retain || ids is not null)
        {
            m = m with
            {
                QoSLevel = (QoSLevel)actualQoS,
                SubscriptionIds = ids,
                Retain = actualRetain
            };
        }

        if (sessionState.OutgoingWriter.TryWrite(m))
        {
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogOutgoingMessage(sessionState.ClientId, UTF8.GetString(m.Topic.Span), m.Payload.Length, actualQoS, false);
        }
    }
}