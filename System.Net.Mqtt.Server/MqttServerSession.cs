using System.Net.Mqtt.Server.Implementations;
using System.Net.Pipes;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSession<T> : MqttServerProtocol
    {
        private readonly IObserver<Message> observer;

        protected MqttServerSession(INetworkTransport transport,
            NetworkPipeReader reader,
            ISessionStateProvider<T> stateProvider,
            IObserver<Message> observer) : base(transport, reader)
        {
            this.observer = observer;
            StateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public ISessionStateProvider<T> StateProvider { get; }

        protected void OnMessageReceived(Message message)
        {
            observer.OnNext(message);
        }
    }
}