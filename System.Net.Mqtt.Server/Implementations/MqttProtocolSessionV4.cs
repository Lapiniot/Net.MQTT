using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolSessionV4 : MqttProtocolSessionV3
    {
        protected internal MqttProtocolSessionV4(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }
    }
}