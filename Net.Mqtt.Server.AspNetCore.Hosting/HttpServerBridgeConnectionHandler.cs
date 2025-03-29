using Microsoft.AspNetCore.Connections;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

/// <summary>
/// Provides generic <see cref="ConnectionHandler"/> which intercepts connections on the specified 
/// connection endpoint and delegate them to the DI-registered <see cref="ITransportConnectionHandler"/> 
/// for further processing.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to query dependency services from.</param>
public sealed class HttpServerBridgeConnectionHandler(IServiceProvider serviceProvider) : ConnectionHandler
{
    public override async Task OnConnectedAsync([NotNull] ConnectionContext connection)
    {
        if (connection is
            {
                LocalEndPoint: { } localEndPoint,
                RemoteEndPoint: { } remoteEndPoint,
                ConnectionClosed: var connectionClosed,
            })
        {
            var handler = serviceProvider.GetRequiredService<ITransportConnectionHandler>();
            var wsatc = new HttpServerAdapterTransportConnection(connection, localEndPoint, remoteEndPoint);
            await using (wsatc.ConfigureAwait(false))
            {
                await handler.OnConnectedAsync(wsatc, connectionClosed).ConfigureAwait(false);
            }
        }
    }

    public async Task OnConnectedAsync([NotNull] MultiplexedConnectionContext multiplexedConnection)
    {
        if (await multiplexedConnection.AcceptAsync().ConfigureAwait(false) is { } connection)
        {
            await OnConnectedAsync(connection).ConfigureAwait(false);
        }
    }
}