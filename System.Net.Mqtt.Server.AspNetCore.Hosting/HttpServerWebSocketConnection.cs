using System.Net.Connections;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal class HttpServerWebSocketConnection : WebSocketConnection<WebSocket>
    {
        private readonly TaskCompletionSource<bool> completionSource;
        private readonly IPEndPoint remoteEndPoint;

        public HttpServerWebSocketConnection(WebSocket socket, IPEndPoint remoteEndPoint) : base(socket)
        {
            this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            completionSource = new TaskCompletionSource<bool>();
        }

        public Task Completion => completionSource.Task;

        public override string ToString()
        {
            return $"{Id}-{nameof(HttpServerWebSocketConnection)}-{remoteEndPoint}";
        }

        #region Overrides of WebSocketConnection<WebSocket>

        public override Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync()
        {
            completionSource.TrySetResult(true);
            return Task.CompletedTask;
        }

        #endregion
    }
}