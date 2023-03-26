namespace System.Net.Mqtt.Server.Protocol.V4;

public sealed class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
{
    private static readonly ReadOnlyMemory<byte> mqttUtf8Str = new[] { (byte)'M', (byte)'Q', (byte)'T', (byte)'T' };
    private readonly int maxInFlight;
    private readonly int maxUnflushedBytes;

    public ProtocolHub(ILogger logger, IMqttAuthenticationHandler authHandler, int maxInFlight, int maxUnflushedBytes) :
        base(logger, authHandler)
    {
        this.maxInFlight = maxInFlight;
        this.maxUnflushedBytes = maxUnflushedBytes;
    }

    public override int ProtocolLevel => 0x04;

    protected override async ValueTask ValidateAsync(ConnectPacket connectPacket, Func<byte, CancellationToken, Task> acknowledge, CancellationToken cancellationToken)
    {
        if (connectPacket.ProtocolLevel != ProtocolLevel || !connectPacket.ProtocolName.Span.SequenceEqual("MQTT"u8))
        {
            await acknowledge(ConnAckPacket.ProtocolRejected, cancellationToken).ConfigureAwait(false);
            UnsupportedProtocolVersionException.Throw(connectPacket.ProtocolLevel);
        }

        if (connectPacket.ClientId.IsEmpty && !connectPacket.CleanSession)
        {
            await acknowledge(ConnAckPacket.IdentifierRejected, cancellationToken).ConfigureAwait(false);
            InvalidClientIdException.Throw();
        }
    }

    protected override MqttServerSession CreateSession([NotNull] ConnectPacket connectPacket, NetworkTransportPipe transport, Observers observers) =>
        new(connectPacket.ClientId.IsEmpty ? Base32.ToBase32String(CorrelationIdGenerator.GetNext()) : UTF8.GetString(connectPacket.ClientId.Span),
            transport, this, Logger, observers, maxUnflushedBytes)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = BuildWillMessage(connectPacket)
        };

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState CreateState(string clientId, bool clean) => new(clientId, DateTime.Now, maxInFlight);

    #endregion
}