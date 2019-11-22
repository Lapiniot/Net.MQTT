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
        private readonly ChannelReader<(WebSocket, IPEndPoint, TaskCompletionSource<bool>)> reader;
        private readonly ChannelWriter<(WebSocket, IPEndPoint, TaskCompletionSource<bool>)> writer;

        public AcceptedWebSocketQueue(IOptions<WebSocketListenerOptions> options)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));

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

        #region Implementation of IAsyncEnumerable<out INetworkConnection>

        public IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return new AcceptedWebSocketEnumerator(reader, cancellationToken);
        }

        private class AcceptedWebSocketEnumerator : IAsyncEnumerator<INetworkConnection>
        {
            private readonly CancellationToken cancellationToken;
            private readonly ChannelReader<(WebSocket, IPEndPoint, TaskCompletionSource<bool>)> reader;
            private INetworkConnection current;

            public AcceptedWebSocketEnumerator(ChannelReader<(WebSocket, IPEndPoint, TaskCompletionSource<bool>)> reader, CancellationToken cancellationToken)
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
                    var (socket, endPoint, source) = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    current = new WebSocketServerConnectionWithSignaling(socket, endPoint, source);
                    return true;
                }
                catch(OperationCanceledException)
                {
                    return false;
                }
            }

            public INetworkConnection Current => current;

            #endregion
        }

        #endregion
    }
}