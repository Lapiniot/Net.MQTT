using System.Net.Mqtt.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public static class WebSocketListenerMiddlewareExtensions
{
    /// <summary>
    /// Adds a web-socket interceptor middleware to the request pipeline based on matches of the given request path.
    /// If the request path starts with a given path the branch with pipeline is executed.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder" /> instance</param>
    /// <param name="pathMatch">The request path to match</param>
    /// <returns>The <see cref="IApplicationBuilder" /> instance</returns>
    public static IApplicationBuilder UseWebSocketInterceptor(this IApplicationBuilder builder, PathString pathMatch)
    {
        return builder.Map(pathMatch, builder => builder.UseMiddleware<WebSocketInterceptorMiddleware>());
    }

    /// <summary>
    /// Adds a web-socket interceptor middleware endpoint with the specified path pattern
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to</param>
    /// <param name="pattern">The route pattern</param>
    /// <returns>A <see cref="IEndpointConventionBuilder" /> that can be used to further customize the endpoint</returns>
    public static IEndpointConventionBuilder MapWebSocketInterceptor(this IEndpointRouteBuilder endpoints, string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.Map(pattern, endpoints
                .CreateApplicationBuilder()
                .UseMiddleware<WebSocketInterceptorMiddleware>()
                .Build())
            .WithDisplayName("MQTT WebSocket Interceptor Middleware");
    }

    public static IMqttHostBuilder AddWebSocketInterceptor(this IMqttHostBuilder builder)
    {
        return AddWebSocketInterceptor(builder, _ => { });
    }

    public static IMqttHostBuilder AddWebSocketInterceptor(this IMqttHostBuilder builder, Action<WebSocketListenerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

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