using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public static class WebSocketListenerMiddlewareExtensions
    {
        public static IServiceCollection AddWebSocketListener(this IServiceCollection services)
        {
            return services
                .AddSingleton<WebSocketListener>()
                .AddTransient<IWebSocketAcceptedNotifier>(ResolveService)
                .AddTransient<IConnectionListener>(ResolveService);

            WebSocketListener ResolveService(IServiceProvider sp)
            {
                return sp.GetRequiredService<WebSocketListener>();
            }
        }

        public static IApplicationBuilder UseWebSocketListener(this IApplicationBuilder builder)
        {
            return builder
                .UseMiddleware<WebSocketMiddleware>()
                .UseMiddleware<WebSocketListenerMiddleware>();
        }
    }
}