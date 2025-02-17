using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Server.Protocol.V3;

public sealed class ProtocolHub4(ILogger logger, IMqttAuthenticationHandler? authHandler, ProtocolOptions options) :
    ProtocolHub3Base<MqttServerSessionState4>(logger, authHandler)
{
    public override int ProtocolLevel => 0x04;

    protected override (Exception?, ReadOnlyMemory<byte>) Validate([NotNull] ConnectPacket connPacket)
    {
        if (connPacket.ProtocolLevel != ProtocolLevel || !connPacket.ProtocolName.Span.SequenceEqual("MQTT"u8))
            return (new UnsupportedProtocolVersionException(connPacket.ProtocolLevel), BuildConnAckPacket(ConnAckPacket.ProtocolRejected));
        else if (connPacket.ClientId.IsEmpty && !connPacket.CleanSession)
            return (new InvalidClientIdException(), BuildConnAckPacket(ConnAckPacket.IdentifierRejected));

        return base.Validate(connPacket);
    }

    protected override MqttServerSession4 CreateSession([NotNull] ConnectPacket connectPacket, TransportConnection connection) =>
        new(connectPacket.ClientId.IsEmpty ? Base32.ToBase32String(CorrelationIdGenerator.GetNext()) : UTF8.GetString(connectPacket.ClientId.Span),
            connection, this, Logger, options.MaxUnflushedBytes, options.MaxInFlight, options.MaxPacketSize)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = BuildWillMessage(connectPacket),
            IncomingObserver = IncomingObserver,
            SubscribeObserver = SubscribeObserver,
            UnsubscribeObserver = UnsubscribeObserver,
            PacketRxObserver = PacketRxObserver,
            PacketTxObserver = PacketTxObserver
        };

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState4 CreateState(string clientId) => new(clientId, DateTime.Now);

    #endregion
}