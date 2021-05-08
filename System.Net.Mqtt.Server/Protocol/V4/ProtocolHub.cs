using Microsoft.Extensions.Logging;

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

        public override Server.MqttServerSession CreateSession(NetworkTransport transport,
            IObserver<SubscriptionRequest> subscribeObserver,
            IObserver<MessageRequest> messageObserver)
        {
            return new MqttServerSession(transport, this, logger, subscribeObserver, messageObserver);
        }

        #region Overrides of MqttProtocolRepositoryHub<SessionState>

        protected override MqttServerSessionState CreateState(string clientId, bool clean)
        {
            return new MqttServerSessionState(clientId, DateTime.Now);
        }

        #endregion
    }
}