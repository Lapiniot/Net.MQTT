using Microsoft.AspNetCore.Connections;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

/// <summary>
/// Provides a bridge between HTTP server connections and transport connection handling.
/// </summary>
/// <param name="handler">The <see cref="ITransportConnectionHandler"/> to delegate connection handling to.</param>
public sealed class HttpServerBridgeConnectionHandler(ITransportConnectionHandler handler) : ConnectionHandler
{
    /// <inheritdoc/>
    public override async Task OnConnectedAsync([NotNull] ConnectionContext connection)
    {
        if (connection is
            {
                LocalEndPoint: { } localEndPoint,
                RemoteEndPoint: { } remoteEndPoint,
                ConnectionClosed: var connectionClosed,
            })
        {
            var adapterConnection = new HttpServerAdapterTransportConnection(connection, localEndPoint, remoteEndPoint);
            await using (adapterConnection.ConfigureAwait(false))
            {
                await handler.OnConnectedAsync(adapterConnection, connectionClosed).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Called when a multiplexed connection is established.
    /// </summary>
    /// <param name="multiplexedConnection">The multiplexed connection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnConnectedAsync([NotNull] MultiplexedConnectionContext multiplexedConnection)
    {
        if (await multiplexedConnection.AcceptAsync().ConfigureAwait(false) is { } connection)
        {
            await OnConnectedAsync(connection).ConfigureAwait(false);
        }
    }
}