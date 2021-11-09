using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public class WebSocketInterceptorMiddleware
{
    private readonly RequestDelegate next;
    private readonly IOptions<WebSocketListenerOptions> options;
    private readonly IAcceptedWebSocketHandler socketHandler;

    public WebSocketInterceptorMiddleware(RequestDelegate next,
        IOptions<WebSocketListenerOptions> options,
        IAcceptedWebSocketHandler socketHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
        this.socketHandler = socketHandler;
        this.next = next;
    }

    public async Task InvokeAsync([NotNull] HttpContext context)
    {
        var manager = context.WebSockets;

        if(socketHandler is not null && manager.IsWebSocketRequest && options.Value.AcceptRules.TryGetValue(context.Request.PathBase, out var rules))
        {
            var match = rules.Intersect(manager.WebSocketRequestedProtocols).FirstOrDefault();

            if(match is not null)
            {
                var socket = await manager.AcceptWebSocketAsync(match).ConfigureAwait(false);
                var connection = context.Connection;
                var localEndPoint = new IPEndPoint(connection.LocalIpAddress, connection.LocalPort);
                var remoteEndPoint = new IPEndPoint(connection.RemoteIpAddress, connection.RemotePort);
                await socketHandler.HandleAsync(socket, localEndPoint, remoteEndPoint, context.RequestAborted).ConfigureAwait(false);
                return;
            }
        }

        await next(context).ConfigureAwait(false);
    }
}