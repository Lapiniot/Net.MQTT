using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using static System.String;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Server.Protocol.V4;

public class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
{
    private readonly int maxPublishInFlight;

    public ProtocolHub(ILogger logger, IMqttAuthenticationHandler authHandler, int maxPublishInFlight) : base(logger, authHandler)
    {
        this.maxPublishInFlight = maxPublishInFlight;
    }

    public override int ProtocolLevel => 0x04;

    protected override async ValueTask ValidateAsync([NotNull] NetworkTransport transport,
        [NotNull] ConnectPacket connectPacket, CancellationToken cancellationToken)
    {
        if(connectPacket.ProtocolLevel != ProtocolLevel || connectPacket.ProtocolName != "MQTT")
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ProtocolRejected }, cancellationToken).ConfigureAwait(false);
            throw new UnsupportedProtocolVersionException(connectPacket.ProtocolLevel);
        }

        if(IsNullOrEmpty(connectPacket.ClientId) && !connectPacket.CleanSession)
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, IdentifierRejected }, cancellationToken).ConfigureAwait(false);
            throw new InvalidClientIdException();
        }
    }

    protected override MqttServerSession CreateSession([NotNull] ConnectPacket connectPacket, Message? willMessage,
        NetworkTransport transport, IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver)
    {
        return new MqttServerSession(connectPacket.ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()),
            transport, this, Logger, subscribeObserver, messageObserver, maxPublishInFlight)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = willMessage
        };
    }

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState CreateState(string clientId, bool clean)
    {
        return new MqttServerSessionState(clientId, DateTime.Now);
    }

    #endregion
}