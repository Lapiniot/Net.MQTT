using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSession : MqttServerProtocol
    {
        private readonly IObserver<Message> observer;
        protected bool ConnectionAccepted;

        protected MqttServerSession(INetworkTransport transport, NetworkPipeReader reader, IObserver<Message> observer) :
            base(transport, reader)
        {
            this.observer = observer;
        }

        public string ClientId { get; set; }

        protected void OnMessageReceived(Message message)
        {
            observer?.OnNext(message);
        }

        public async Task AcceptConnectionAsync(CancellationToken cancellationToken)
        {
            await OnAcceptConnectionAsync(cancellationToken).ConfigureAwait(false);

            ConnectionAccepted = true;
        }

        protected abstract Task OnAcceptConnectionAsync(CancellationToken cancellationToken);

        public abstract Task CloseSessionAsync();
    }

    public abstract class MqttServerSession<T> : MqttServerSession where T : SessionState
    {
        protected MqttServerSession(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<T> stateProvider, IObserver<Message> observer) :
            base(transport, reader, observer)
        {
            StateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public ISessionStateProvider<T> StateProvider { get; }
    }
}