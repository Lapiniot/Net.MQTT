namespace System.Net.Mqtt.Server.Protocol.V4;

public class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
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

    protected override async ValueTask ValidateAsync([NotNull] NetworkTransportPipe transport,
        [NotNull] ConnectPacket connectPacket, CancellationToken cancellationToken)
    {
        if (connectPacket.ProtocolLevel != ProtocolLevel || !connectPacket.ProtocolName.Span.SequenceEqual("MQTT"u8))
        {
            await transport.Output.WriteAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.ProtocolRejected }, cancellationToken).ConfigureAwait(false);
            UnsupportedProtocolVersionException.Throw(connectPacket.ProtocolLevel);
        }

        if (connectPacket.ClientId.IsEmpty && !connectPacket.CleanSession)
        {
            await transport.Output.WriteAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.IdentifierRejected }, cancellationToken).ConfigureAwait(false);
            InvalidClientIdException.Throw();
        }
    }

    protected override MqttServerSession CreateSession([NotNull] ConnectPacket connectPacket, Message? willMessage,
        NetworkTransportPipe transport, IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver) =>
        new(connectPacket.ClientId.IsEmpty ? Base32.ToBase32String(CorrelationIdGenerator.GetNext()) : UTF8.GetString(connectPacket.ClientId.Span),
            transport, this, Logger, subscribeObserver, messageObserver, maxUnflushedBytes)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = willMessage
        };

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState CreateState(string clientId, bool clean) => new(clientId, DateTime.Now, maxInFlight);

    #endregion
}