using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public static class WebSocketsListenerMiddlewareExtensions
    {
        public static IServiceCollection AddWebSocketListener(this IServiceCollection services)
        {
            return services.AddSingleton<IWebSocketAcceptedNotifier, WebSocketListenerEnumerator>();
        }

        public static IApplicationBuilder UseWebSocketListener(this IApplicationBuilder builder)
        {
            return builder
                .UseMiddleware<WebSocketMiddleware>()
                .UseMiddleware<WebSocketListenerMiddleware>();
        }
    }
}