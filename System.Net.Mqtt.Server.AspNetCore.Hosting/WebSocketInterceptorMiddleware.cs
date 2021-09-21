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
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.socketHandler = socketHandler;
        this.next = next;
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Do not check for null, parameter value will be provided by infrastructure")]
    public async Task InvokeAsync(HttpContext context)
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