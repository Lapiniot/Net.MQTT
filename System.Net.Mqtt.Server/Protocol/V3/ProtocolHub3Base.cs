using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public abstract class ProtocolHub3Base<TSessionState> : MqttProtocolHubWithRepository<TSessionState, ConnectPacket>
    where TSessionState : MqttServerSessionState
{
    private readonly IMqttAuthenticationHandler? authHandler;

    protected ProtocolHub3Base(ILogger logger, IMqttAuthenticationHandler? authHandler) : base(logger) =>
        this.authHandler = authHandler;

    public required IObserver<IncomingMessage> IncomingObserver { get; init; }
    public required IObserver<SubscribeMessage> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }

    protected static Message? BuildWillMessage([NotNull] ConnectPacket packet) =>
        !packet.WillTopic.IsEmpty ? new(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain) : null;

    protected static byte[] BuildConnAckPacket(byte reasonCode) => new byte[] { 0b0010_0000, 2, 0, reasonCode };

    protected override (Exception?, ReadOnlyMemory<byte>) Validate(ConnectPacket? connPacket)
    {
        return connPacket is null
            ? (new MissingConnectPacketException(), ReadOnlyMemory<byte>.Empty)
            : authHandler is null || authHandler.Authenticate(UTF8.GetString(connPacket.UserName.Span), UTF8.GetString(connPacket.Password.Span))
            ? (null, ReadOnlyMemory<byte>.Empty)
            : (new InvalidCredentialsException(), BuildConnAckPacket(ConnAckPacket.CredentialsRejected));
    }
}