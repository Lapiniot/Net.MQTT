using System.Net;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using OOs.Net.Connections;
using static System.Threading.Channels.BoundedChannelFullMode;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public class WebSocketInterceptorListener : IAsyncEnumerable<TransportConnection>, IAcceptedWebSocketHandler
{
    private readonly string addresses;
    private readonly ChannelReader<TransportConnection> reader;
    private readonly ChannelWriter<TransportConnection> writer;

    public WebSocketInterceptorListener([NotNull] IOptions<WebSocketInterceptorOptions> options, [NotNull] IServer server)
    {
        var channel = Channel.CreateBounded<TransportConnection>(
            new BoundedChannelOptions(options.Value.QueueCapacity) { SingleReader = true, SingleWriter = false, FullMode = Wait });

        reader = channel.Reader;
        writer = channel.Writer;
        addresses = server.Features.Get<IServerAddressesFeature>() is { Addresses: { } collection } ? $"({string.Join(";", collection)})" : string.Empty;
    }

    public override string ToString() => $"{nameof(WebSocketInterceptorListener)} {addresses}";

    #region Implementation of IAcceptedWebSocketHandler

    public async ValueTask HandleAsync(WebSocket webSocket, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        var connection = new KestrelWebSocketTransportConnection(webSocket, localEndPoint, remoteEndPoint);
        await using (connection.ConfigureAwait(false))
        {
            await writer.WriteAsync(connection, cancellationToken).ConfigureAwait(false);
            await connection.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Implementation of IAsyncEnumerable<INetworkConnection>

    public async IAsyncEnumerator<TransportConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TransportConnection connection;
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