using System.IO.Pipelines;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class SessionFactory : MqttSessionFactory
    {
        public override int ProtocolVersion => 0x03;

        public override MqttServerSession Create(IMqttServer server, INetworkTransport transport, PipeReader reader)
        {
            return new ServerSession(server, transport, reader);
        }
    }
}