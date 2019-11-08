using System.IO.Pipelines;
using System.Net.Connections;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class ProtocolHub : MqttProtocolRepositoryHub<MqttServerSessionState>
    {
        private readonly ILogger logger;

        public ProtocolHub(ILogger logger) : base(logger)
        {
            this.logger = logger;
        }

        public override int ProtocolVersion => 0x03;

        public override Server.MqttServerSession CreateSession(IMqttServer server, INetworkConnection connection, PipeReader reader)
        {
            return new MqttServerSession(server, connection, reader, this, logger);
        }

        #region Overrides of MqttProtocolRepositoryHub<SessionState>

        protected override MqttServerSessionState CreateState(string clientId, bool clean)
        {
            return new MqttServerSessionState(clientId, DateTime.Now);
        }

        #endregion
    }
}