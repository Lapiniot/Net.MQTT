﻿using System.Net.Mqtt.Server.Exceptions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
{
    private readonly int maxInFlight;

    public ProtocolHub(ILogger logger, IMqttAuthenticationHandler authHandler, int maxInFlight) :
        base(logger, authHandler) => this.maxInFlight = maxInFlight;

    public override int ProtocolLevel => 0x03;

    protected override async ValueTask ValidateAsync([NotNull] NetworkTransport transport,
        [NotNull] ConnectPacket connectPacket, CancellationToken cancellationToken)
    {
        if (connectPacket.ProtocolLevel != ProtocolLevel)
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.ProtocolRejected }, cancellationToken).ConfigureAwait(false);
            UnsupportedProtocolVersionException.Throw(connectPacket.ProtocolLevel);
        }

        if (connectPacket.ClientId.Length is 0 or > 23)
        {
            await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ConnAckPacket.IdentifierRejected }, cancellationToken).ConfigureAwait(false);
            InvalidClientIdException.Throw();
        }
    }

    protected override MqttServerSession CreateSession([NotNull] ConnectPacket connectPacket, Message? willMessage,
        NetworkTransport transport, IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver) =>
        new(UTF8.GetString(connectPacket.ClientId.Span), transport, this, Logger, subscribeObserver, messageObserver)
        {
            CleanSession = connectPacket.CleanSession,
            KeepAlive = connectPacket.KeepAlive,
            WillMessage = willMessage
        };

    #region Overrides of MqttProtocolRepositoryHub<SessionState>

    protected override MqttServerSessionState CreateState(string clientId, bool clean) => new(clientId, DateTime.Now, maxInFlight);

    #endregion
}