using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttServerSessionV5 : MqttServerSessionV4
    {
        public MqttServerSessionV5(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionStateV5> stateProvider) :
            base(transport, reader, stateProvider)
        {
        }
    }
}