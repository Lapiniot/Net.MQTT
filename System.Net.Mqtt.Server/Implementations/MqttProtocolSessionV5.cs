using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolSessionV5 : MqttProtocolSessionV4
    {
        public MqttProtocolSessionV5(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<ProtocolStateV5> stateProvider) :
            base(transport, reader, stateProvider)
        {
        }
    }
}