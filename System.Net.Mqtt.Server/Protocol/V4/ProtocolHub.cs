using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.String;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class ProtocolHub : MqttProtocolHubWithRepository<MqttServerSessionState>
    {
        private readonly ILogger logger;

        public ProtocolHub(ILogger logger) : base(logger)
        {
            this.logger = logger;
        }

        public override int ProtocolVersion => 0x04;

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
                if(connPack.ProtocolLevel != ProtocolVersion || connPack.ProtocolName != "MQTT")
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ProtocolRejected }, cancellationToken).ConfigureAwait(false);
                    throw new UnsupportedProtocolVersionException(connPack.ProtocolLevel);
                }

                if(IsNullOrEmpty(connPack.ClientId) && !connPack.CleanSession)
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, IdentifierRejected }, cancellationToken).ConfigureAwait(false);
                    throw new InvalidClientIdException();
                }

                var willMessage = !IsNullOrEmpty(connPack.WillTopic)
                    ? new Message(connPack.WillTopic, connPack.WillMessage, connPack.WillQoS, connPack.WillRetain)
                    : null;

                var session = new MqttServerSession(connPack.ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()),
                    transport, this, logger, subscribeObserver, messageObserver)
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