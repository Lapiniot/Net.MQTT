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
        private readonly IOptions<WebSocketListenerOptions> options;
        private readonly IAcceptedWebSocketHandler socketHandler;

        public WebSocketListenerMiddleware(RequestDelegate next,
            IOptions<WebSocketListenerOptions> options,
            IAcceptedWebSocketHandler socketHandler)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.socketHandler = socketHandler ?? throw new ArgumentNullException(nameof(socketHandler));
            this.next = next;
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Do not check for null, parameter value will be provided by infrastructure")]
        public async Task InvokeAsync(HttpContext context)
        {
            var manager = context.WebSockets;

            if(manager.IsWebSocketRequest && options.Value.AcceptRules.TryGetValue(context.Request.PathBase, out var rules))
            {
                var match = rules.Intersect(manager.WebSocketRequestedProtocols).FirstOrDefault();

                if(match is not null)
                {
                    var socket = await manager.AcceptWebSocketAsync(match).ConfigureAwait(false);
                    var connection = context.Connection;
                    var remoteEndPoint = new IPEndPoint(connection.RemoteIpAddress ?? throw new InvalidOperationException("Remote IP cannot be null"), connection.RemotePort);
                    await socketHandler.HandleAsync(socket, remoteEndPoint, context.RequestAborted).ConfigureAwait(false);
                    return;
                }
            }

            await next(context).ConfigureAwait(false);
        }
    }
}