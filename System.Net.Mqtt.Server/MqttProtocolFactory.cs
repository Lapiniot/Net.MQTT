using System.IO.Pipelines;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttProtocolFactory
    {
        public abstract int ProtocolVersion { get; }
        public abstract MqttServerSession CreateSession(IMqttServer server, INetworkTransport transport, PipeReader reader);
        public abstract void NotifyMessage(Message message);
    }
}