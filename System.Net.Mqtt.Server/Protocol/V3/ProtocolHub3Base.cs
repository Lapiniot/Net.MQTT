using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public abstract class ProtocolHub3Base<TSessionState> : MqttProtocolHubWithRepository<Message3, TSessionState, ConnectPacket, PublishDeliveryState>
    where TSessionState : MqttServerSessionState3
{
    private readonly IMqttAuthenticationHandler? authHandler;

    protected ProtocolHub3Base(ILogger logger, IMqttAuthenticationHandler? authHandler) : base(logger) =>
        this.authHandler = authHandler;

    public required IObserver<IncomingMessage3> IncomingObserver { get; init; }
    public required IObserver<SubscribeMessage3> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }

    protected static Message3? BuildWillMessage([NotNull] ConnectPacket packet) =>
        !packet.WillTopic.IsEmpty ? new(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain) : null;

    protected static byte[] BuildConnAckPacket(byte reasonCode) => new byte[] { 0b0010_0000, 2, 0, reasonCode };

    protected override (Exception?, ReadOnlyMemory<byte>) Validate([NotNull] ConnectPacket connPacket)
    {
        return authHandler is null || authHandler.Authenticate(UTF8.GetString(connPacket.UserName.Span), UTF8.GetString(connPacket.Password.Span))
            ? (null, ReadOnlyMemory<byte>.Empty)
            : (new InvalidCredentialsException(), BuildConnAckPacket(ConnAckPacket.CredentialsRejected));
    }

    protected sealed override void Dispatch([NotNull] TSessionState sessionState, Message3 message)
    {
        var (topic, payload, qos, _) = message;

        if (!sessionState.IsActive && qos == 0)
        {
            // Skip all incoming QoS 0 if session is inactive
            return;
        }

        if (!sessionState.TopicMatches(topic.Span, out var maxQoS))
        {
            return;
        }

        var adjustedQoS = Math.Min(qos, maxQoS);

        if (sessionState.OutgoingWriter.TryWrite(qos == adjustedQoS ? message : message with { QoSLevel = adjustedQoS }))
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogOutgoingMessage(sessionState.ClientId, UTF8.GetString(topic.Span), payload.Length, adjustedQoS, false);
            }
        }
    }
}