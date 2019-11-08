using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public class WebSocketListenerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly WebSocketListenerOptions options;
        private readonly PathString pathBase;
        private readonly IAcceptedWebSocketQueue socketQueue;

        public WebSocketListenerMiddleware(RequestDelegate next, IOptions<WebSocketListenerOptions> options,
            IAcceptedWebSocketQueue socketQueue, PathString pathBase = default)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));
            this.next = next;
            this.options = options.Value;
            this.socketQueue = socketQueue;
            this.pathBase = pathBase;
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Do not check for null, parameter value will be provided by infrastructure")]
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