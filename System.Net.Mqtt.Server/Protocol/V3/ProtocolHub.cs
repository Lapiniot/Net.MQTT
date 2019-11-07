using System.IO.Pipelines;
using System.Net.Connections;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class ProtocolHub : MqttProtocolRepositoryHub<SessionState>
    {
        public ProtocolHub(ILogger logger) : base(logger) {}

        public override int ProtocolVersion => 0x03;

        public override MqttServerSession CreateSession(IMqttServer server, INetworkConnection connection, PipeReader reader)
        {
            return new ServerSession(server, connection, reader, this, Logger);
        }

        #region Overrides of MqttProtocolRepositoryHub<SessionState>

        protected override SessionState CreateState(string clientId, bool clean)
        {
            return new SessionState(clientId, DateTime.Now);
        }

        #endregion
    }
}