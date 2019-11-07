using System.Collections.Generic;
using System.Net.Connections;
using System.Net.Listeners;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using static System.Threading.Channels.BoundedChannelFullMode;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public class AcceptedWebSocketQueue : ConnectionListener, IAcceptedWebSocketQueue
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

        #region Overrides of ConnectionListener

        protected override async IAsyncEnumerable<INetworkConnection> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var (socket, remoteEndPoint, completionSource) = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                yield return new WebSocketServerConnectionWithSignaling(socket, remoteEndPoint, completionSource);
            }
        }

        #endregion
    }
}