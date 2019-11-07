using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal class WebSocketListenerMiddleware
    {
        private readonly ILogger<WebSocketListenerMiddleware> logger;
        private readonly PathString pathBase;
        private readonly RequestDelegate next;
        private readonly WebSocketListenerOptions options;
        private readonly IAcceptedWebSocketQueue socketQueue;

        internal WebSocketListenerMiddleware(RequestDelegate next, IOptions<WebSocketListenerOptions> options,
            IAcceptedWebSocketQueue socketQueue, ILogger<WebSocketListenerMiddleware> logger, PathString pathBase = default)
        {
            this.next = next;
            this.options = options.Value;
            this.socketQueue = socketQueue;
            this.logger = logger;
            this.pathBase = pathBase;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var manager = context.WebSockets;

            if(manager.IsWebSocketRequest && options.AcceptRules.TryGetValue(pathBase.HasValue ? pathBase : context.Request.Path, out var rule))
            {
                var intersect = rule.Intersect(manager.WebSocketRequestedProtocols).ToArray();

                if(intersect.Length > 0)
                {
                    var socket = await manager.AcceptWebSocketAsync(intersect[0]).ConfigureAwait(false);
                    var connection = context.Connection;

                    var awaiter = await socketQueue.EnqueueAsync(socket, new IPEndPoint(connection.RemoteIpAddress, connection.RemotePort), context.RequestAborted).ConfigureAwait(false);

                    await awaiter.ConfigureAwait(false);

                    return;
                }

            }

            await next(context).ConfigureAwait(false);
        }
    }
}