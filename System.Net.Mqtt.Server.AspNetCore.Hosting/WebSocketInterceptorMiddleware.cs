namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

#pragma warning disable CA1812 // Avoid not instantiated internal classes - instantiated by DI container
internal class WebSocketInterceptorMiddleware : IMiddleware
{
    private readonly IAcceptedWebSocketHandler handler;
    private readonly IOptionsSnapshot<WebSocketInterceptorOptions> options;

    public WebSocketInterceptorMiddleware(IAcceptedWebSocketHandler handler, IOptionsSnapshot<WebSocketInterceptorOptions> options)
    {
        this.handler = handler;
        this.options = options;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var manager = context.WebSockets;
        var path = context.Request.PathBase + context.Request.Path;

        if(manager.IsWebSocketRequest && options.Value.AcceptRules.TryGetValue(path, out var rules) &&
            rules.Intersect(manager.WebSocketRequestedProtocols).FirstOrDefault() is { } subProtocol)
        {
            var socket = await manager.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false);
            var connection = context.Connection;
            var localEndPoint = new IPEndPoint(connection.LocalIpAddress!, connection.LocalPort);
            var remoteEndPoint = new IPEndPoint(connection.RemoteIpAddress!, connection.RemotePort);
            await handler.HandleAsync(socket, localEndPoint, remoteEndPoint, context.RequestAborted).ConfigureAwait(false);
            await context.Response.CompleteAsync().ConfigureAwait(false);
        }
        else
        {
            // Request doesn't pass acceptance precondition
            if(context.GetEndpoint() is null)
            {
                // We sit in the application request pipeline, so pass request to the next delegate in the pipeline, 
                await next(context).ConfigureAwait(false);
            }
            else
            {
                // We act as a part of the endpoint pipeline, so short-circuit and complete request
                await context.Response.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}