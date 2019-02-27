using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSession : MqttServerProtocol
    {
        protected readonly IMqttServer Server;
        protected bool ConnectionAccepted;

        protected MqttServerSession(IMqttServer server, INetworkTransport transport, PipeReader reader, ILogger logger) : base(transport, reader)
        {
            Logger = logger;
            Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        protected ILogger Logger { get; }
        public string ClientId { get; set; }

        public async Task AcceptConnectionAsync(CancellationToken cancellationToken)
        {
            await OnAcceptConnectionAsync(cancellationToken).ConfigureAwait(false);

            ConnectionAccepted = true;
        }

        protected abstract Task OnAcceptConnectionAsync(CancellationToken cancellationToken);

        protected void OnMessageReceived(Message message)
        {
            Server.OnMessage(message, ClientId);
        }
    }
}