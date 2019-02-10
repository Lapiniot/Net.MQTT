using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSession : MqttServerProtocol
    {
        protected readonly IMqttServer Server;
        protected bool ConnectionAccepted;

        protected MqttServerSession(IMqttServer server, INetworkTransport transport, PipeReader reader) : base(transport, reader)
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
}