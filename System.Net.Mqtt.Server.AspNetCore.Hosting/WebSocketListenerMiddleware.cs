using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal class WebSocketListenerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IWebSocketAcceptedNotifier notifier;

        public WebSocketListenerMiddleware(RequestDelegate next, IWebSocketAcceptedNotifier notifier)
        {
            this.next = next;
            this.notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
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