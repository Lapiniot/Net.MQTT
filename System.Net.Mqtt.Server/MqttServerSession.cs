using System.IO.Pipelines;
using System.Net.Connections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSession : MqttServerProtocol
    {
        private readonly IMqttServer server;

        protected MqttServerSession(IMqttServer server, INetworkConnection connection, PipeReader reader, ILogger logger) : base(connection, reader)
        {
            Logger = logger;
            this.server = server ?? throw new ArgumentNullException(nameof(server));
        }

        protected bool ConnectionAccepted { get; set; }
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
            server.OnMessage(message, ClientId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return StartActivityAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task StopAsync()
        {
            return StopActivityAsync();
        }
    }
}