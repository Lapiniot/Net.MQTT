﻿using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class ProtocolHub3 : ProtocolHub3Base<MqttServerSessionState3>
{
    private readonly int maxInFlight;
    private readonly int maxUnflushedBytes;

    public ProtocolHub3(ILogger logger, IMqttAuthenticationHandler? authHandler, int maxInFlight, int maxUnflushedBytes) :
        base(logger, authHandler)
    {
        this.maxInFlight = maxInFlight;
        this.maxUnflushedBytes = maxUnflushedBytes;
    }

    public override int ProtocolLevel => 0x03;

    protected override (Exception?, ReadOnlyMemory<byte>) Validate([NotNull] ConnectPacket connPacket)
    {
        if (connPacket.ProtocolLevel != ProtocolLevel)
            return (new UnsupportedProtocolVersionException(connPacket.ProtocolLevel), BuildConnAckPacket(ConnAckPacket.ProtocolRejected));
        else if (connPacket.ClientId.Length is 0 or > 23)
            return (new InvalidClientIdException(), BuildConnAckPacket(ConnAckPacket.IdentifierRejected));

        return base.Validate(connPacket);
    }

    protected override MqttServerSession3 CreateSession([NotNull] ConnectPacket connectPacket, NetworkTransportPipe transport) =>
        new(UTF8.GetString(connectPacket.ClientId.Span), transport, this, Logger, maxUnflushedBytes)
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

    protected override MqttServerSessionState3 CreateState(string clientId) => new(clientId, DateTime.Now, maxInFlight);

    #endregion
}