using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolV4 : MqttProtocolV3
    {
        protected internal MqttProtocolV4(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }
    }
}