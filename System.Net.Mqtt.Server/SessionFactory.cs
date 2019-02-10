using System.IO.Pipelines;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttSessionFactory
    {
        public abstract int ProtocolVersion { get; }
        public abstract MqttServerSession Create(IMqttServer server, INetworkTransport transport, PipeReader reader);
    }
}