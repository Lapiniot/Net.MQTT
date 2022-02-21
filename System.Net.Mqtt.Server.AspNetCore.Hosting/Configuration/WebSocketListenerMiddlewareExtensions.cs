using System.Net.Mqtt.Server.Hosting;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting.Configuration;

public static class WebSocketListenerMiddlewareExtensions
{
    /// <summary>
    /// Adds a web-socket interceptor middleware to the request pipeline based on matches of the given request path.
    /// If the request path starts with a given path the branch with pipeline is executed.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder" /> instance</param>
    /// <param name="pathMatch">The request path to match</param>
    /// <returns>The <see cref="IApplicationBuilder" /> instance</returns>
    public static IApplicationBuilder UseWebSocketInterceptor(this IApplicationBuilder builder, PathString pathMatch) => builder.Map(pathMatch, b => b.UseMiddleware<WebSocketInterceptorMiddleware>());

    /// <summary>
    /// Adds a web-socket interceptor middleware endpoint with the specified path pattern
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder" /> to add the route to</param>
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

    /// <summary>
    /// Registers web-socket interceptor middleware and related options in the DI container
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to</param>
    /// <returns>A reference to this instance after the operation has completed</returns>
    public static IServiceCollection AddWebSocketInterceptor(this IServiceCollection services)
    {
        services.AddOptions<WebSocketInterceptorOptions>().BindConfiguration("WSListener");
        return services.AddTransient<WebSocketInterceptorMiddleware>();
    }

    /// <summary>
    /// Registers web-socket interceptor middleware and related options in the DI container
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to</param>
    /// <param name="configureOptions">The action used to configure <see cref="WebSocketInterceptorOptions" /> instance</param>
    /// <returns>A reference to this instance after the operation has completed</returns>
    public static IServiceCollection AddWebSocketInterceptor(this IServiceCollection services, Action<WebSocketInterceptorOptions> configureOptions) =>
        services.AddWebSocketInterceptor().Configure(configureOptions);

    /// <summary>
    /// Registers web-sockets listener adapter, which serves as glue layer between
    /// web-socket interceptor middleware and MQTT server connection listener infrastructure
    /// </summary>
    /// <param name="builder">The instance of <see cref="IMqttHostBuilder" /> to be configured</param>
    /// <returns>A reference to this instance after the operation has completed</returns>
    public static IMqttHostBuilder AddWebSocketInterceptorListener(this IMqttHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((_, services) => services
                .AddSingleton<WebSocketInterceptorListener>()
                .AddTransient<IAcceptedWebSocketHandler>(ResolveAdapterService))
            .ConfigureOptions((_, options) => options.ListenerFactories.Add("aspnet.websockets", ResolveAdapterService));
    }

    private static WebSocketInterceptorListener ResolveAdapterService(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<WebSocketInterceptorListener>();
}