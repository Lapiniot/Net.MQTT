using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal class WebSocketListenerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IWebSocketAcceptedNotifier notifier;
        private readonly ILogger<WebSocketListenerMiddleware> logger;

        public WebSocketListenerMiddleware(RequestDelegate next, IWebSocketAcceptedNotifier notifier, ILogger<WebSocketListenerMiddleware> logger)
        {
            this.next = next;
            this.notifier = notifier;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if(context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                await notifier.NotifyAcceptedAsync(socket, context.RequestAborted).ConfigureAwait(false);
            }

            await next(context).ConfigureAwait(false);
        }
    }
}