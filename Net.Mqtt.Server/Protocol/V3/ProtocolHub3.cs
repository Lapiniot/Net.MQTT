using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Server.Protocol.V3;

public sealed class ProtocolHub3(ILogger logger, IMqttAuthenticationHandler? authHandler, ProtocolOptions options) :
    ProtocolHub3Base<MqttServerSessionState3>(logger, authHandler)
{
    public override int ProtocolLevel => 0x03;

    protected override ValueTask<(Exception?, ReadOnlyMemory<byte>)> ValidateAsync([NotNull] ConnectPacket connPacket)
    {
        if (connPacket.ProtocolLevel != ProtocolLevel)
        {
            return new ValueTask<(Exception?, ReadOnlyMemory<byte>)>((
                new UnsupportedProtocolVersionException(connPacket.ProtocolLevel),
                BuildConnAckPacket(ConnAckPacket.ProtocolRejected)));
        }
        else if (connPacket.ClientId.Length is 0 or > 23)
        {
            return new ValueTask<(Exception?, ReadOnlyMemory<byte>)>((
                new InvalidClientIdException(),
                BuildConnAckPacket(ConnAckPacket.IdentifierRejected)));
        }

        return base.ValidateAsync(connPacket);
    }

    protected override MqttServerSession3 CreateSession([NotNull] ConnectPacket connectPacket, TransportConnection connection) =>
        new(UTF8.GetString(connectPacket.ClientId.Span), connection, this, Logger, options.MaxUnflushedBytes, options.MaxInFlight, options.MaxPacketSize)
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

    protected override MqttServerSessionState3 CreateState(string clientId) => new(clientId, DateTime.Now);

    #endregion
}