using Microsoft.Extensions.DependencyInjection.Extensions;
using Net.Mqtt.Server.Hosting;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public static class HttpServerIntegrationExtensions
{
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ConnectionQueueListenerOptions))]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static IServiceCollection AddWebSocketInterceptor(this IServiceCollection services)
    {
        services.AddOptions<WebSocketInterceptorOptions>().BindConfiguration("WSListener");
        services.TryAddTransient<IValidateOptions<WebSocketInterceptorOptions>, WebSocketInterceptorOptionsValidator>();
        services.TryAddTransient<WebSocketInterceptorMiddleware>();
        return services;
    }

    /// <summary>
    /// Registers web-sockets listener adapter, which serves as glue layer between
    /// web-socket interceptor middleware and MQTT server connection listener infrastructure
    /// </summary>
    /// <param name="builder">The instance of <see cref="OptionsBuilder{MqttServerOptions}" /> to be configured</param>
    /// <param name="name">Endpoint display name.</param>
    /// <returns>A reference to this instance after the operation has completed</returns>
    public static void UseHttpServerWebSocketConnections(this MqttServerOptionsBuilder builder, string name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddWebSocketInterceptor();
        builder.UseHttpServerIntegration(name);
    }

    internal static void UseHttpServerIntegration(this MqttServerOptionsBuilder builder, string name = null)
    {
        builder.Services.AddOptions<ConnectionQueueListenerOptions>().BindConfiguration("WSListener");
        builder.Services.TryAddTransient<IValidateOptions<ConnectionQueueListenerOptions>, ConnectionQueueListenerOptionsValidator>();
        builder.Services.TryAddSingleton<ConnectionQueueListener>();
        builder.Services.TryAddSingleton<ITransportConnectionHandler>(serviceProvider => serviceProvider.GetRequiredService<ConnectionQueueListener>());
        builder.Builder.Configure<ConnectionQueueListener>(ConfigureOptions);

        void ConfigureOptions(MqttServerOptions options, ConnectionQueueListener listener) =>
            options.Endpoints[name ?? "aspnet.connections"] = new(() => listener);
    }
}