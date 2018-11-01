using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolSessionV5 : MqttProtocolSessionV4
    {
        protected internal MqttProtocolSessionV5(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
        }
    }
}