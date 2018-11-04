using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Implementations
{
    public class MqttServerSessionV4 : MqttServerSessionV3
    {
        public MqttServerSessionV4(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionStateV3> stateProvider, IObserver<Message> observer) :
            base(transport, reader, stateProvider, observer)
        {
        }
    }
}