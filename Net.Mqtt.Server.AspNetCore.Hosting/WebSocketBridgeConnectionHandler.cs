using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

#pragma warning disable CA1812

/// <summary>
/// Provides a bridge between WebSocket connections and transport connection handling.
/// </summary>
/// <param name="handler">The <see cref="ITransportConnectionHandler"/> to delegate connection handling to.</param>
internal sealed class WebSocketBridgeConnectionHandler(ITransportConnectionHandler handler) : ConnectionHandler
{
    /// <inheritdoc/>
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        if (connection.Features.Get<IHttpContextFeature>() is
            {
                HttpContext:
                {
                    WebSockets.IsWebSocketRequest: true,
                    Connection:
                    {
                        LocalIpAddress: { } localAddress, LocalPort: var localPort,
                        RemoteIpAddress: { } remoteAddress, RemotePort: var remotePort
                    }
                }
            })
        {
            if (connection.Features.Get<ITransferFormatFeature>() is { } transportFormatFeature &&
                (transportFormatFeature.SupportedFormats & TransferFormat.Binary) is not 0)
            {
                transportFormatFeature.ActiveFormat = TransferFormat.Binary;
            }

            var adapterConnection = new HttpServerAdapterTransportConnection(connection,
                localEndPoint: new IPEndPoint(localAddress, localPort),
                remoteEndPoint: new IPEndPoint(remoteAddress, remotePort));
            await using (adapterConnection.ConfigureAwait(false))
            {
                await handler.OnConnectedAsync(adapterConnection, connection.ConnectionClosed).ConfigureAwait(false);
            }
        }
    }
}