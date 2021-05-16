using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.String;
using static System.Net.Mqtt.Packets.ConnAckPacket;
using System.Security.Authentication;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
    {
        private readonly ILogger logger;
        private readonly IMqttAuthenticationHandler authHandler;

        public ProtocolHub(ILogger logger, IMqttAuthenticationHandler authHandler = null) : base(logger)
        {
            this.logger = logger;
            this.authHandler = authHandler;
        }

        public override int ProtocolVersion => 0x03;

        public override async Task<Server.MqttServerSession> AcceptConnectionAsync(
            NetworkTransport transport,
            IObserver<SubscriptionRequest> subscribeObserver,
            IObserver<MessageRequest> messageObserver,
            CancellationToken cancellationToken)
        {
            var reader = (transport ?? throw new ArgumentNullException(nameof(transport))).Reader;
            var rt = MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken);
            var prr = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

            if(ConnectPacket.TryRead(prr.Buffer, out var connPack, out _))
            {
                if(connPack.ProtocolLevel != ProtocolVersion)
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ProtocolRejected }, cancellationToken).ConfigureAwait(false);
                    throw new UnsupportedProtocolVersionException(connPack.ProtocolLevel);
                }

                if(IsNullOrEmpty(connPack.ClientId) || connPack.ClientId.Length > 23)
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, IdentifierRejected }, cancellationToken).ConfigureAwait(false);
                    throw new InvalidClientIdException();
                }

                if(authHandler?.Authenticate(connPack.UserName, connPack.Password) == false)
                {
                    throw new AuthenticationException();
                }

                var willMessage = !IsNullOrEmpty(connPack.WillTopic)
                    ? new Message(connPack.WillTopic, connPack.WillMessage, connPack.WillQoS, connPack.WillRetain)
                    : null;

                var session = new MqttServerSession(connPack.ClientId, transport, this, logger,
                    subscribeObserver, messageObserver)
                {
                    CleanSession = connPack.CleanSession,
                    KeepAlive = connPack.KeepAlive,
                    WillMessage = willMessage
                };

                reader.AdvanceTo(prr.Buffer.End);

                return session;
            }
            else
            {
                throw new MissingConnectPacketException();
            }
        }

        #region Overrides of MqttProtocolRepositoryHub<SessionState>

        protected override MqttServerSessionState CreateState(string clientId, bool clean)
        {
            return new MqttServerSessionState(clientId, DateTime.Now);
        }

        #endregion
    }
}