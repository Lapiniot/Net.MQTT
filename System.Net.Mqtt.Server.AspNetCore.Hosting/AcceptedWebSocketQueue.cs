using System.Collections.Generic;
using System.Net.Connections;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using static System.Threading.Channels.BoundedChannelFullMode;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public class AcceptedWebSocketQueue : IAsyncEnumerable<INetworkConnection>, IAcceptedWebSocketQueue
    {
        private readonly ChannelReader<WebSocketConnection> reader;
        private readonly ChannelWriter<WebSocketConnection> writer;

        public AcceptedWebSocketQueue(IOptions<WebSocketListenerOptions> options)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));

            var channel = Channel.CreateBounded<WebSocketConnection>(
                new BoundedChannelOptions(options.Value.QueueCapacity) { SingleReader = true, SingleWriter = false, FullMode = Wait });

            reader = channel.Reader;
            writer = channel.Writer;
        }

        #region Implementation of IAcceptedWebSocketQueue

        public async ValueTask<Task> EnqueueAsync(WebSocket webSocket, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            var connection = new WebSocketConnection(webSocket, remoteEndPoint);
            await writer.WriteAsync(connection, cancellationToken).ConfigureAwait(false);
            return connection.Completion;
        }

        #endregion

        #region Implementation of IAsyncEnumerable<out INetworkConnection>

        public IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return new AcceptedWebSocketEnumerator(reader, cancellationToken);
        }

        private class AcceptedWebSocketEnumerator : IAsyncEnumerator<INetworkConnection>
        {
            private readonly CancellationToken cancellationToken;
            private readonly ChannelReader<WebSocketConnection> reader;

            public AcceptedWebSocketEnumerator(ChannelReader<WebSocketConnection> reader, CancellationToken cancellationToken)
            {
                this.reader = reader;
                this.cancellationToken = cancellationToken;
            }

            #region Implementation of IAsyncDisposable

            public ValueTask DisposeAsync()
            {
                return default;
            }

            #endregion

            #region Implementation of IAsyncEnumerator<out INetworkConnection>

            public async ValueTask<bool> MoveNextAsync()
            {
                try
                {
                    Current = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch(OperationCanceledException)
                {
                    return false;
                }
            }

            public INetworkConnection Current { get; private set; }

            #endregion
        }

        #endregion

        private class WebSocketConnection : WebSocketConnection<WebSocket>
        {
            private readonly IPEndPoint remoteEndPoint;
            private readonly TaskCompletionSource<bool> completionSource;

            public WebSocketConnection(WebSocket socket, IPEndPoint remoteEndPoint) : base(socket)
            {
                this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
                completionSource = new TaskCompletionSource<bool>();
            }

            public override string ToString()
            {
                return $"{nameof(WebSocketConnection)}: {remoteEndPoint}";
            }

            public Task Completion => completionSource.Task;

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
}