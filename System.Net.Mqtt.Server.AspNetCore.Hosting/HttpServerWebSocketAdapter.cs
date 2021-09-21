using System.Net.Connections;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

using static System.Threading.Channels.BoundedChannelFullMode;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public class HttpServerWebSocketAdapter : IAsyncEnumerable<INetworkConnection>, IAcceptedWebSocketHandler
{
    private readonly ChannelReader<HttpServerWebSocketConnection> reader;
    private readonly ChannelWriter<HttpServerWebSocketConnection> writer;
    private readonly ICollection<string> addresses;

    public HttpServerWebSocketAdapter(IOptions<WebSocketListenerOptions> options, IServer server)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(server);

        addresses = server.Features.Get<IServerAddressesFeature>().Addresses;

        var channel = Channel.CreateBounded<HttpServerWebSocketConnection>(
            new BoundedChannelOptions(options.Value.QueueCapacity) { SingleReader = true, SingleWriter = false, FullMode = Wait });

        reader = channel.Reader;
        writer = channel.Writer;
    }

    #region Implementation of IAcceptedWebSocketHandler

    public async ValueTask HandleAsync(WebSocket webSocket, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        var connection = new HttpServerWebSocketConnection(webSocket, localEndPoint, remoteEndPoint);
        await using(connection.ConfigureAwait(false))
        {
            var vt = writer.WriteAsync(connection, cancellationToken);

            if(!vt.IsCompletedSuccessfully)
            {
                await vt.ConfigureAwait(false);
            }

            await connection.Completion.ConfigureAwait(false);
        }
    }

    #endregion

    #region Implementation of IAsyncEnumerable<INetworkConnection>

    public async IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while(!cancellationToken.IsCancellationRequested)
        {
            INetworkConnection connection;
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
        return $"{nameof(HttpServerWebSocketAdapter)} {{{string.Join(", ", addresses)}}}";
    }
}