using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class ProtocolHub4 : ProtocolHub3Base<MqttServerSessionState4>
{
    private static readonly ReadOnlyMemory<byte> mqttUtf8Str = new[] { (byte)'M', (byte)'Q', (byte)'T', (byte)'T' };
    private readonly int maxInFlight;
    private readonly int maxUnflushedBytes;

    public ProtocolHub4(ILogger logger, IMqttAuthenticationHandler authHandler, int maxInFlight, int maxUnflushedBytes, TimeSpan connectTimeout) :
        base(logger, authHandler, connectTimeout)
    {
        this.maxInFlight = maxInFlight;
        this.maxUnflushedBytes = maxUnflushedBytes;
    }

    public override int ProtocolLevel => 0x04;

    protected override (Exception?, ReadOnlyMemory<byte>) Validate(ConnectPacket? connPacket)
    {
        if (connPacket is not null)
        {
            if (connPacket.ProtocolLevel != ProtocolLevel || !connPacket.ProtocolName.Span.SequenceEqual("MQTT"u8))
                return (new UnsupportedProtocolVersionException(connPacket.ProtocolLevel), BuildConnAckPacket(ConnAckPacket.ProtocolRejected));
            else if (connPacket.ClientId.IsEmpty && !connPacket.CleanSession)
                return (new InvalidClientIdException(), BuildConnAckPacket(ConnAckPacket.IdentifierRejected));
        }

        return base.Validate(connPacket);
    }

    protected override MqttServerSession4 CreateSession([NotNull] ConnectPacket connectPacket, NetworkTransportPipe transport, Observers observers) =>
        new(connectPacket.ClientId.IsEmpty ? Base32.ToBase32String(CorrelationIdGenerator.GetNext()) : UTF8.GetString(connectPacket.ClientId.Span),
            transport, this, Logger, observers, maxUnflushedBytes)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = BuildWillMessage(connectPacket)
        };

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState4 CreateState(string clientId, bool clean) => new(clientId, DateTime.Now, maxInFlight);

    #endregion
}