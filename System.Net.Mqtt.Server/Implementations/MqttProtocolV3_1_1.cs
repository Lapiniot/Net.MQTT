using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolV3_1_1 : MqttProtocolV3_1_0
    {
        protected internal MqttProtocolV3_1_1(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }
    }
}