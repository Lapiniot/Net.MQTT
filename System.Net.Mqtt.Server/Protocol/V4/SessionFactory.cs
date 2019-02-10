using System.IO.Pipelines;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class SessionFactory : MqttSessionFactory
    {
        public override int ProtocolVersion => 0x04;

        public override MqttServerSession Create(IMqttServer server, INetworkTransport transport, PipeReader reader)
        {
            return new ServerSession(server, transport, reader);
        }
    }
}