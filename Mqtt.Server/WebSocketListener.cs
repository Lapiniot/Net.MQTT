using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Mqtt.Server
{
    internal class WebSocketListenerMiddleware
    {
        private readonly RequestDelegate next;

        public WebSocketListenerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            // Call the next delegate/middleware in the pipeline
            await next(context);
        }
    }
}