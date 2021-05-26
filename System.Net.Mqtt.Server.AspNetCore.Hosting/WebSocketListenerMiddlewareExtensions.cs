using System.Net.Mqtt.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    public static class WebSocketListenerMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketInterceptor(this IApplicationBuilder builder)
        {
            return builder
                .UseMiddleware<WebSocketMiddleware>()
                .UseMiddleware<WebSocketInterceptorMiddleware>();
        }

        public static IApplicationBuilder UseWebSocketInterceptor(this IApplicationBuilder builder, PathString pathMatch)
        {
            return builder.Map(pathMatch, builder => builder
                .UseMiddleware<WebSocketMiddleware>()
                .UseMiddleware<WebSocketInterceptorMiddleware>());
        }

        public static IMqttHostBuilder UseWebSocketInterceptor(this IMqttHostBuilder builder)
        {
            return UseWebSocketInterceptor(builder, _ => { });
        }

        public static IMqttHostBuilder UseWebSocketInterceptor(this IMqttHostBuilder builder, Action<WebSocketListenerOptions> configureOptions)
        {
            if(builder is null) throw new ArgumentNullException(nameof(builder));

            builder.ConfigureServices((ctx, services) => services
                .Configure<WebSocketListenerOptions>(ctx.Configuration.GetSection("WSListener"))
                .PostConfigure(configureOptions)
                .AddSingleton<HttpServerWebSocketAdapter>()
                .AddTransient<IAcceptedWebSocketHandler>(ResolveAdapterService));

            builder.ConfigureOptions((ctx, options) => options.ListenerFactories.Add("aspnet.websockets", ResolveAdapterService));

            return builder;
        }

        private static HttpServerWebSocketAdapter ResolveAdapterService(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<HttpServerWebSocketAdapter>();
        }
    }
}