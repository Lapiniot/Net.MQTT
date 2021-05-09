using System.Collections.Generic;
using System.Net.Connections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public static class WebSocketListenerMiddlewareExtensions
    {
        public static IServiceCollection AddWebSocketListener(this IServiceCollection services)
        {
            return services
                .AddSingleton<HttpServerWebSocketAdapter>()
                .AddTransient<IAcceptedWebSocketHandler>(ResolveService)
                .AddTransient<IAsyncEnumerable<INetworkConnection>>(ResolveService);

            static HttpServerWebSocketAdapter ResolveService(IServiceProvider sp)
            {
                return sp.GetRequiredService<HttpServerWebSocketAdapter>();
            }
        }

        public static IServiceCollection AddWebSocketListener(this IServiceCollection services, Action<WebSocketListenerOptions> configure)
        {
            return services
                .Configure(configure)
                .AddWebSocketListener();
        }

        public static IServiceCollection AddWebSocketListener(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .Configure<WebSocketListenerOptions>(configuration)
                .AddWebSocketListener();
        }

        public static IServiceCollection AddWebSocketListener(this IServiceCollection services, IConfiguration configuration, Action<WebSocketListenerOptions> configure)
        {
            return services
                .Configure<WebSocketListenerOptions>(configuration)
                .PostConfigure(configure)
                .AddWebSocketListener();
        }

        public static IApplicationBuilder UseWebSocketListener(this IApplicationBuilder builder)
        {
            return builder
                .UseMiddleware<WebSocketMiddleware>()
                .UseMiddleware<WebSocketListenerMiddleware>();
        }

        public static IApplicationBuilder UseWebSocketListener(this IApplicationBuilder builder, PathString pathMatch)
        {
            return builder.Map(pathMatch, b => b
                .UseMiddleware<WebSocketMiddleware>()
                .UseMiddleware<WebSocketListenerMiddleware>(pathMatch));
        }
    }
}