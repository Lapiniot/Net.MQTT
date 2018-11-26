using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSession : MqttServerProtocol
    {
        protected readonly IMqttServer Server;
        protected bool ConnectionAccepted;

        protected MqttServerSession(INetworkTransport transport, NetworkPipeReader reader, IMqttServer server) :
            base(transport, reader)
        {
            Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public string ClientId { get; set; }

        public async Task AcceptConnectionAsync(CancellationToken cancellationToken)
        {
            await OnAcceptConnectionAsync(cancellationToken).ConfigureAwait(false);

            ConnectionAccepted = true;
        }

        protected abstract Task OnAcceptConnectionAsync(CancellationToken cancellationToken);

        protected void OnMessageReceived(Message message)
        {
            Server.OnMessage(message);
        }
    }

    public abstract class MqttServerSession<T> : MqttServerSession where T : SessionState
    {
        protected MqttServerSession(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<T> stateProvider, IMqttServer server) :
            base(transport, reader, server)
        {
            StateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public ISessionStateProvider<T> StateProvider { get; }
    }
}