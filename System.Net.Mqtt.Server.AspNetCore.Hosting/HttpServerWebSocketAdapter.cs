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
    public class HttpServerWebSocketAdapter : IAsyncEnumerable<INetworkConnection>, IAcceptedWebSocketHandler
    {
        private readonly ChannelReader<HttpServerWebSocketConnection> reader;
        private readonly ChannelWriter<HttpServerWebSocketConnection> writer;

        public HttpServerWebSocketAdapter(IOptions<WebSocketListenerOptions> options)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));

            var channel = Channel.CreateBounded<HttpServerWebSocketConnection>(
                new BoundedChannelOptions(options.Value.QueueCapacity) { SingleReader = true, SingleWriter = false, FullMode = Wait });

            reader = channel.Reader;
            writer = channel.Writer;
        }

        #region Implementation of IAcceptedWebSocketHandler

        public async ValueTask HandleAsync(WebSocket webSocket, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            await using var connection = new HttpServerWebSocketConnection(webSocket, remoteEndPoint);

            var vt = writer.WriteAsync(connection, cancellationToken);
            if(!vt.IsCompletedSuccessfully)
            {
                await vt.ConfigureAwait(false);
            }

            await connection.Completion.ConfigureAwait(false);
        }

        #endregion

        #region Implementation of IAsyncEnumerable<INetworkConnection>

        public async IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                INetworkConnection connection = null;

                try
                {
                    connection = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                    break;
                }

                if(connection is not null)
                {
                    yield return connection;
                }
            }
        }

        #endregion

        public override string ToString()
        {
            return nameof(HttpServerWebSocketAdapter);
        }
    }
}