using System.Collections.Generic;
using System.Net.Listeners;
using System.Net.Transports;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using static System.Threading.Channels.BoundedChannelFullMode;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal class AcceptedWebSocketQueue : ConnectionListener, IAcceptedWebSocketQueue
    {
        private readonly ChannelReader<(WebSocket, IPEndPoint, TaskCompletionSource<bool>)> reader;
        private readonly ChannelWriter<(WebSocket, IPEndPoint, TaskCompletionSource<bool>)> writer;

        public AcceptedWebSocketQueue(IOptions<WebSocketListenerOptions> options)
        {
            var channel = Channel.CreateBounded<(WebSocket, IPEndPoint, TaskCompletionSource<bool>)>(
                new BoundedChannelOptions(options.Value.QueueCapacity) {SingleReader = true, SingleWriter = false, FullMode = Wait});
            reader = channel.Reader;
            writer = channel.Writer;
        }

        #region Implementation of IAcceptedWebSocketQueue

        public async ValueTask<Task> EnqueueAsync(WebSocket webSocket, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<bool>();
            await writer.WriteAsync((webSocket, remoteEndPoint, completionSource), cancellationToken).ConfigureAwait(false);
            return completionSource.Task;
        }

        #endregion

        #region Overrides of ConnectionListener

        protected override async IAsyncEnumerable<INetworkConnection> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var (socket, remoteEndPoint, completionSource) = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                yield return new WebSocketServerConnection(socket, remoteEndPoint, completionSource);
            }
        }

        #endregion

        private class WebSocketServerConnection : WebSocketConnection<WebSocket>
        {
            private readonly IPEndPoint remoteEndPoint;
            private readonly TaskCompletionSource<bool> taskCompletionSource;

            public WebSocketServerConnection(WebSocket socket, IPEndPoint remoteEndPoint, TaskCompletionSource<bool> taskCompletionSource) : base(socket)
            {
                this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
                this.taskCompletionSource = taskCompletionSource ?? throw new ArgumentNullException(nameof(taskCompletionSource));
            }

            public override string ToString()
            {
                return $"{nameof(WebSocketServerConnection)}: {remoteEndPoint}";
            }

            #region Overrides of WebSocketConnection<WebSocket>

            public override Task ConnectAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public override async Task DisconnectAsync()
            {
                try
                {
                    await base.DisconnectAsync().ConfigureAwait(false);
                }
                finally
                {
                    taskCompletionSource.TrySetResult(true);
                }
            }

            #endregion
        }
    }
}