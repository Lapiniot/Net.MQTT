﻿using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class ProtocolHub3 : ProtocolHub3Base<MqttServerSessionState3>
{
    private readonly int maxInFlight;
    private readonly int maxUnflushedBytes;

    public ProtocolHub3(ILogger logger, IMqttAuthenticationHandler authHandler, int maxInFlight, int maxUnflushedBytes) :
        base(logger, authHandler)
    {
        this.maxInFlight = maxInFlight;
        this.maxUnflushedBytes = maxUnflushedBytes;
    }

    public override int ProtocolLevel => 0x03;

    protected override async ValueTask ValidateAsync([NotNull] ConnectPacket connectPacket, [NotNull] Func<byte, CancellationToken, Task> acknowledge, CancellationToken cancellationToken)
    {
        if (connectPacket.ProtocolLevel != ProtocolLevel)
        {
            await acknowledge(ConnAckPacket.ProtocolRejected, cancellationToken).ConfigureAwait(false);
            UnsupportedProtocolVersionException.Throw(connectPacket.ProtocolLevel);
        }

        if (connectPacket.ClientId.Length is 0 or > 23)
        {
            await acknowledge(ConnAckPacket.IdentifierRejected, cancellationToken).ConfigureAwait(false);
            InvalidClientIdException.Throw();
        }
    }

    protected override MqttServerSession3 CreateSession([NotNull] ConnectPacket connectPacket, NetworkTransportPipe transport, Observers observers) =>
        new(UTF8.GetString(connectPacket.ClientId.Span), transport, this, Logger, observers, maxUnflushedBytes)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = BuildWillMessage(connectPacket)
        };

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState3 CreateState(string clientId, bool clean) => new(clientId, DateTime.Now, maxInFlight);

    #endregion
}