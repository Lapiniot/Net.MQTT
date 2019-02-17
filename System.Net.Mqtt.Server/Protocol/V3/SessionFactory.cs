using System.IO.Pipelines;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class MqttProtocolFactory : MqttProtocolFactoryWithRepository<SessionState>
    {
        private readonly ILogger logger;

        public MqttProtocolFactory(ILogger logger)
        {
            this.logger = logger;
        }

        public override int ProtocolVersion => 0x03;

        public override MqttServerSession CreateSession(IMqttServer server, INetworkTransport transport, PipeReader reader)
        {
            return new ServerSession(server, transport, reader, this, logger);
        }

        #region Overrides of MqttProtocolFactoryWithRepository<SessionState>

        protected override SessionState CreateState(string clientId, bool clean)
        {
            return new SessionState(clientId, DateTime.Now);
        }

        #endregion
    }
}