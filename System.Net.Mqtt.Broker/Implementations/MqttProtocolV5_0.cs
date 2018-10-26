using System.Net.Pipes;

namespace System.Net.Mqtt.Broker.Implementations
{
    internal class MqttProtocolV5_0 : MqttProtocol
    {
        public MqttProtocolV5_0(NetworkPipeReader reader) : base(reader)
        {
        }
    }
}