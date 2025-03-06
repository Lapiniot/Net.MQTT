using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

#pragma warning disable CA1812

/// <summary>
/// Provides specialized <see cref="ConnectionHandler"/> which intercepts only WebSocket connections on the specified 
/// connection endpoint and delegate them to the provided <see cref="ITransportConnectionHandler"/> 
/// for further processing.
/// </summary>
/// <param name="connectionHandler">The <see cref="ITransportConnectionHandler"/> to process connection by.</param>
internal sealed class WebSocketBridgeConnectionHandler(ITransportConnectionHandler connectionHandler) : ConnectionHandler
{
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

            var wsatc = new HttpServerAdapterTransportConnection(connection,
                localEndPoint: new IPEndPoint(localAddress, localPort),
                remoteEndPoint: new IPEndPoint(remoteAddress, remotePort));
            await using (wsatc.ConfigureAwait(false))
            {
                await connectionHandler.OnConnectedAsync(wsatc, connection.ConnectionClosed).ConfigureAwait(false);
                await wsatc.Completion.ConfigureAwait(false);
            }
        }
    }
}