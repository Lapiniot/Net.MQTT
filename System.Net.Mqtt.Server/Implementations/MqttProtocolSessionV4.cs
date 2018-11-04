using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttProtocolSessionV4 : MqttProtocolSessionV3
    {
        public MqttProtocolSessionV4(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionStateV4> stateProvider) :
            base(transport, reader, stateProvider)
        {
        }
    }
}