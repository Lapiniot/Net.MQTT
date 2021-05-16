using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class MqttServerSession : V3.MqttServerSession
    {
        public MqttServerSession(string clientId, NetworkTransport transport,
            ISessionStateRepository<V3.MqttServerSessionState> stateRepository, ILogger logger,
            IObserver<SubscriptionRequest> subscribeObserver,
            IObserver<MessageRequest> messageObserver) :
            base(clientId, transport, stateRepository, logger, subscribeObserver, messageObserver)
        {
        }

        #region Overrides of ServerSession

        protected override ValueTask<int> AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(new byte[] { 0b0010_0000, 2, (byte)(existing ? 1 : 0), Accepted }, cancellationToken);
        }

        #endregion
    }
}