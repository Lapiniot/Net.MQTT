using System.Net.Connections;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using static System.Threading.Channels.BoundedChannelFullMode;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public class WebSocketInterceptorListener : IAsyncEnumerable<NetworkConnection>, IAcceptedWebSocketHandler
{
    private readonly string addresses;
    private readonly ChannelReader<HttpServerWebSocketConnection> reader;
    private readonly ChannelWriter<HttpServerWebSocketConnection> writer;

    public WebSocketInterceptorListener([NotNull] IOptions<WebSocketInterceptorOptions> options, [NotNull] IServer server)
    {
        var channel = Channel.CreateBounded<HttpServerWebSocketConnection>(
            new BoundedChannelOptions(options.Value.QueueCapacity) { SingleReader = true, SingleWriter = false, FullMode = Wait });

        reader = channel.Reader;
        writer = channel.Writer;
        addresses = server.Features.Get<IServerAddressesFeature>() is { Addresses: { } collection } ? $"({string.Join(";", collection)})" : string.Empty;
    }

    public override string ToString() => $"{nameof(WebSocketInterceptorListener)} {addresses}";

    #region Implementation of IAcceptedWebSocketHandler

    public async ValueTask HandleAsync(WebSocket webSocket, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        var connection = new HttpServerWebSocketConnection(webSocket, localEndPoint, remoteEndPoint);
        await using (connection.ConfigureAwait(false))
        {
            await writer.WriteAsync(connection, cancellationToken).ConfigureAwait(false);
            await connection.Completion.ConfigureAwait(false);
        }
    }

    #endregion

    #region Implementation of IAsyncEnumerable<INetworkConnection>

    public async IAsyncEnumerator<NetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            NetworkConnection connection;
            try
            {
                connection = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            yield return connection;
        }
    }

    #endregion
}