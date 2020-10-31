using System.IO.Pipelines;
using System.Net.Connections;
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

        public override Server.MqttServerSession CreateSession(INetworkConnection connection, PipeReader reader,
            IObserver<(Server.MqttServerSessionState State, (string topic, byte qosLevel)[] Filters)> subscribeObserver,
            IObserver<(Message Message, string ClientId)> messageObserver)
        {
            return new MqttServerSession(connection, reader, this, logger, subscribeObserver, messageObserver);
        }

        #region Overrides of MqttProtocolRepositoryHub<SessionState>

        protected override MqttServerSessionState CreateState(string clientId, bool clean)
        {
            return new MqttServerSessionState(clientId, DateTime.Now);
        }

        #endregion
    }
}