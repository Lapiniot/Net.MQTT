using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolV5 : MqttProtocolV4
    {
        protected internal MqttProtocolV5(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }
    }
}