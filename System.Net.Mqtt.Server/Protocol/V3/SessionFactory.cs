using System.IO.Pipelines;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class MqttProtocolFactory : MqttProtocolFactoryWithRepository<SessionState>
    {
        public override int ProtocolVersion => 0x03;

        public override MqttServerSession CreateSession(IMqttServer server, INetworkTransport transport, PipeReader reader)
        {
            return new ServerSession(server, transport, reader, this);
        }

        #region Overrides of MqttProtocolFactoryWithRepository<SessionState>

        protected override SessionState CreateState(string clientId, bool clean)
        {
            return new SessionState(clientId, DateTime.Now);
        }

        #endregion
    }
}