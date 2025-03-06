using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Net.Mqtt.Server.Hosting;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public static class HttpServerIntegrationExtensions
{
    private const string ConfigSectionPath = "Kestrel-MQTT";

    /// <summary>
    /// Maps incoming requests with the specified <paramref name="pattern"/> to the provided connection 
    /// pipeline built by <paramref name="endpoints"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder" /> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder" /> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapMqttWebSockets(this IEndpointRouteBuilder endpoints,
        string? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        pattern ??= "/mqtt";
        var wso = endpoints.ServiceProvider.GetRequiredService<IOptions<WebSocketInterceptorOptions>>();

        return endpoints
            .MapConnectionHandler<WebSocketBridgeConnectionHandler>(pattern, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.WebSockets.SubProtocolSelector = SelectSubProtocol;
            })
            .WithDisplayName("MQTT WebSocket Interceptor");

        string SelectSubProtocol(IList<string> clientSubProtocols) =>
            wso.Value.AcceptProtocols.TryGetValue(pattern, out var serverSubProtocols)
                ? clientSubProtocols.FirstOrDefault(sp => serverSubProtocols.Contains(sp), "")
                : "mqtt";
    }

    /// <summary>
    /// Use connections from this endpoint for MQTT server bridge listener (enables integration 
    /// between Kestrel and MQTT server on this connection pipeline).
    /// </summary>
    /// <param name="options">Kestrel's <see cref="IConnectionBuilder"/>.</param>
    /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
    public static IConnectionBuilder UseMqttServer(this IConnectionBuilder options) =>
        options.UseConnectionHandler<HttpServerBridgeConnectionHandler>();

    /// <summary>
    /// Registers <see cref="WebSocketInterceptorMiddleware"/> and related services in the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to</param>
    /// <returns>A reference to this instance after the operation has completed</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ConnectionQueueListenerOptions))]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static IServiceCollection AddWebSocketInterceptor(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<WebSocketInterceptorOptions, WebSocketInterceptorOptionsValidator>()
            .BindConfiguration(ConfigSectionPath);
        services.TryAddTransient<WebSocketInterceptorMiddleware>();
        return services;
    }

    /// <summary>
    /// Registers web-sockets listener adapter, which serves as a 'glue layer' between
    /// web-socket interceptor middleware and MQTT server connection listener infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="MqttServerOptionsBuilder"/>.</param>
    /// <param name="endPointName">Endpoint display name.</param>
    /// <returns>The <see cref="MqttServerOptionsBuilder"/>.</returns>
    public static MqttServerOptionsBuilder UseHttpServerWebSocketConnections(
        this MqttServerOptionsBuilder builder, string? endPointName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddWebSocketInterceptor();
        builder.UseHttpServerIntegration(endPointName);
        return builder;
    }

    /// <summary>
    /// Registers all requred 'glue layer' services to enable HTTP server integration.
    /// </summary>
    /// <param name="builder">The <see cref="MqttServerOptionsBuilder"/>.</param>
    /// <param name="endPointName">The MQTT listener endpoint name.</param>
    /// <returns>The <see cref="MqttServerOptionsBuilder"/>.</returns>
    public static MqttServerOptionsBuilder UseHttpServerIntegration(this MqttServerOptionsBuilder builder,
        string? endPointName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddConnectionQueueListener(endPointName);
        return builder;
    }

    /// <summary>
    /// Registers all requred 'glue layer' services to enable MQTT server integration.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseMqttIntegration(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ConfigureServices(services => services.AddConnectionQueueListener());
    }

    /// <summary>
    /// Registers all requred 'glue layer' services to enable HTTP server and MQTT server integration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="endPointName">The MQTT listener endpoint name.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddConnectionQueueListener(this IServiceCollection services,
        string? endPointName = null)
    {
        services.AddOptions<MqttServerOptions>().Configure<ConnectionQueueListener>(ConfigureOptions);
        services
            .AddOptionsWithValidateOnStart<ConnectionQueueListenerOptions, ConnectionQueueListenerOptionsValidator>()
            .BindConfiguration(ConfigSectionPath);

        services.TryAddSingleton<ConnectionQueueListener>();
        services.TryAddSingleton<ITransportConnectionHandler>(
            serviceProvider => serviceProvider.GetRequiredService<ConnectionQueueListener>());

        void ConfigureOptions(MqttServerOptions options, ConnectionQueueListener listener) =>
            options.Endpoints[endPointName ?? "aspnet.connections"] = new(() => listener);

        return services;
    }
}