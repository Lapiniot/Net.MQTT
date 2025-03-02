using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using OOs.Net.Connections;
using static System.Threading.Channels.BoundedChannelFullMode;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

#pragma warning disable CA1812
internal sealed class ConnectionQueueListener :
    IAsyncEnumerable<TransportConnection>,
    ITransportConnectionHandler
{
    private readonly IServer server;
    private readonly ChannelReader<TransportConnection> reader;
    private readonly ChannelWriter<TransportConnection> writer;
    private string? addresses;

    public ConnectionQueueListener(IOptions<ConnectionQueueListenerOptions> options, IServer server)
    {
        this.server = server;

        var channel = Channel.CreateBounded<TransportConnection>(
            new BoundedChannelOptions(options.Value.QueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = Wait
            });

        reader = channel.Reader;
        writer = channel.Writer;
    }

    public override string ToString()
    {
        addresses ??= server.Features.Get<IServerAddressesFeature>() is { Addresses: { Count: > 0 } collection }
            ? string.Join("; ", collection)
            : null;

        return $"{nameof(ConnectionQueueListener)} ({addresses})";
    }

    #region Implementation of IAcceptedWebSocketHandler

    public async ValueTask OnConnectedAsync(TransportConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await writer.WriteAsync(connection, cancellationToken).ConfigureAwait(false);
            await connection.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected
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