using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSession : MqttServerProtocol
    {
        private readonly IObserver<MessageRequest> messageObserver;

        protected MqttServerSession(NetworkTransport transport, ILogger logger, IObserver<MessageRequest> messageObserver) : base(transport)
        {
            Logger = logger;
            this.messageObserver = messageObserver;
        }

        protected bool ConnectionAccepted { get; set; }
        protected ILogger Logger { get; }
        public string ClientId { get; set; }
        public bool DisconnectReceived { get; protected set; }

        public async Task AcceptConnectionAsync(CancellationToken cancellationToken)
        {
            await OnAcceptConnectionAsync(cancellationToken).ConfigureAwait(false);

            ConnectionAccepted = true;
        }

        protected abstract Task OnAcceptConnectionAsync(CancellationToken cancellationToken);

        protected void OnMessageReceived(Message message)
        {
            messageObserver?.OnNext(new MessageRequest(message, ClientId));
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

        public override string ToString()
        {
            return $"'{ClientId}' over '{Transport}'";
        }
    }
}