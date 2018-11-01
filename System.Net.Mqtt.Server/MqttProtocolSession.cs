using System.Net.Pipes;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttProtocolSession<T> : MqttProtocol
    {
        protected MqttProtocolSession(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<T> stateProvider) : base(transport, reader)
        {
            StateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public ISessionStateProvider<T> StateProvider { get; }
    }
}