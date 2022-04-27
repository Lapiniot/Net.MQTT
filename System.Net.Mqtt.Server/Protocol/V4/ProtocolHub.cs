using System.Diagnostics.CodeAnalysis;
using System.Net.Mqtt.Server.Exceptions;

namespace System.Net.Mqtt.Server.Protocol.V4;

public class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
{
    private static readonly ReadOnlyMemory<byte> MqttUtf8Str = new byte[] { (byte)'M', (byte)'Q', (byte)'T', (byte)'T' };
    private readonly int maxInFlight;

    public ProtocolHub(ILogger logger, IMqttAuthenticationHandler authHandler, int maxInFlight) :
        base(logger, authHandler) => this.maxInFlight = maxInFlight;

    public override int ProtocolLevel => 0x04;

    protected override async ValueTask ValidateAsync([NotNull] NetworkTransport transport,
        [NotNull] ConnectPacket connectPacket, CancellationToken cancellationToken)
    {
        if (connectPacket.ProtocolLevel != ProtocolLevel || !connectPacket.ProtocolName.Span.SequenceEqual(MqttUtf8Str.Span))
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.ProtocolRejected }, cancellationToken).ConfigureAwait(false);
            throw new UnsupportedProtocolVersionException(connectPacket.ProtocolLevel);
        }

        if (connectPacket.ClientId.IsEmpty && !connectPacket.CleanSession)
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.IdentifierRejected }, cancellationToken).ConfigureAwait(false);
            throw new InvalidClientIdException();
        }
    }

    protected override MqttServerSession CreateSession([NotNull] ConnectPacket connectPacket, Message? willMessage,
        NetworkTransport transport, IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver) =>
        new(connectPacket.ClientId.IsEmpty ? Base32.ToBase32String(CorrelationIdGenerator.GetNext()) : UTF8.GetString(connectPacket.ClientId.Span),
            transport, this, Logger, subscribeObserver, messageObserver)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = willMessage
        };

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState CreateState(string clientId, bool clean) => new(clientId, DateTime.Now, maxInFlight);

    #endregion
}