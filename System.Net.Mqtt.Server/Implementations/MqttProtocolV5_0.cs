using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolV5_0 : MqttProtocolV3_1_1
    {
        protected internal MqttProtocolV5_0(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }
    }
}