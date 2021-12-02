using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using static System.String;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
{
    public ProtocolHub(ILogger logger, IMqttAuthenticationHandler authHandler, int connectTimeout) :
        base(logger, authHandler, connectTimeout: connectTimeout)
    {
    }

    public override int ProtocolLevel => 0x03;

    protected override async ValueTask ValidateAsync([NotNull] NetworkTransport transport,
        [NotNull] ConnectPacket connectPacket, CancellationToken cancellationToken)
    {
        if(connectPacket.ProtocolLevel != ProtocolLevel)
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ProtocolRejected }, cancellationToken).ConfigureAwait(false);
            throw new UnsupportedProtocolVersionException(connectPacket.ProtocolLevel);
        }

        if(IsNullOrEmpty(connectPacket.ClientId) || connectPacket.ClientId.Length > 23)
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, IdentifierRejected }, cancellationToken).ConfigureAwait(false);
            throw new InvalidClientIdException();
        }
    }

    protected override MqttServerSession CreateSession([NotNull] ConnectPacket connectPacket, Message? willMessage,
        NetworkTransport transport, IObserver<SubscriptionRequest> subscribeObserver, IObserver<MessageRequest> messageObserver)
    {
        return new MqttServerSession(connectPacket.ClientId, transport,
            this, Logger, subscribeObserver, messageObserver)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = willMessage,
        };
    }

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState CreateState(string clientId, bool clean)
    {
        return new MqttServerSessionState(clientId, DateTime.Now);
    }

    #endregion
}