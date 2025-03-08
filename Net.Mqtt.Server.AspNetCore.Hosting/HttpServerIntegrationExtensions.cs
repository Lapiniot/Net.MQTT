using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
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
        var optionsMonitor = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<WebSocketConnectionOptions>>();
        var wscOptions = optionsMonitor.Get(pattern);

        // ConnectionEndpointRouteBuilderExtensions.MapConnectionHandler() calls 
        // ConnectionEndpointRouteBuilderExtensions.MapConnections() under the hood, which maps 
        // two additional endpoints ('{pattern}' & '{pattern}/negotiate') implicitly. 
        // Both endpoints heavily rely on WebSocketsMiddleware presence in their execution pipeline.
        // In order to ensure this middleware is present, the latter method registers it explicitly via calls to the 
        // parameterless overload of WebSocketMiddlewareExtensions.UseWebSockets(). No WebSocketOptions provided to the
        // middleware upon its registration means it will grab these options from the ApplicationServices DI container. 
        // We don't want it to use shared instance of the IOptions<WebSocketOptions> because there could be interference
        // with other APIs that rely on UseWebSockets() and configure this 'global' options instance 
        // according to the own need. 
        // For example, SignalRDependencyInjectionExtensions.AddSignalR()  has this code:
        //      // Disable the WebSocket keep alive since SignalR has it's own
        //      services.Configure<WebSocketOptions>(o => o.KeepAliveInterval = TimeSpan.Zero);
        // Thus, we need to ensure that our connection handling pipeline uses own correct version of the 
        // WebSocketsMiddleware with pre-configured WebSocketOptions passed down deliberately, and this middleware sits 
        // in the very beginning of the created connection endpoint handling pipeline.
        // EndpointRouteBuilderProxy allows us to inject correct instance at the very early moment when 
        // endpoints.CreateApplicationBuilder() is being called by some underlying code. 
        // This approach also allows us to support per-endpoint configuration overrides.
        var endpointRouteBuilderProxy = new EndpointRouteBuilderProxy<WebSocketConnectionOptions>(endpoints,
            static (builder, options) => builder.UseWebSockets(options), wscOptions);

        return endpointRouteBuilderProxy
            .MapConnectionHandler<WebSocketBridgeConnectionHandler>(pattern, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.WebSockets.CloseTimeout = wscOptions.CloseTimeout;
                options.WebSockets.SubProtocolSelector = SelectSubProtocol;
            })
            .WithDisplayName($"MQTT WebSocket Handler: {{{pattern}}}");

        string SelectSubProtocol(IList<string> clientSubProtocols) =>
            clientSubProtocols.FirstOrDefault(sp => wscOptions.SubProtocols.Contains(sp), "");
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
    /// Registers <see cref="WebSocketBridgeConnectionHandler"/> and related services in the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to</param>
    /// <returns>A reference to this instance after the operation has completed</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ConnectionQueueListenerOptions))]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static IServiceCollection AddWebSocketsHandler(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<WebSocketConnectionOptions, WebSocketConnectionOptionsValidator>();
        services.TryAddTransient<IConfigureOptions<WebSocketConnectionOptions>>(
            implementationFactory: static sp => new WebSocketConnectionOptionsSetup(
                sp.GetRequiredService<IConfiguration>().GetSection($"{ConfigSectionPath}:WebSockets")));
        services.TryAddSingleton<WebSocketBridgeConnectionHandler>();
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
        builder.Services.AddWebSocketsHandler();
        builder.UseHttpServerIntegration(endPointName);
        return builder;
    }

    /// <summary>
    /// Registers all required 'glue layer' services to enable HTTP server integration.
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
    /// Registers all required 'glue layer' services to enable MQTT server integration.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseMqttIntegration(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ConfigureServices(services => services.AddConnectionQueueListener());
    }

    /// <summary>
    /// Registers all required 'glue layer' services to enable HTTP server and MQTT server integration.
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

#pragma warning disable CA1812
    private sealed class WebSocketConnectionOptionsSetup(IConfiguration configuration) :
        IConfigureNamedOptions<WebSocketConnectionOptions>
    {
        public void Configure(string? name, WebSocketConnectionOptions options)
        {
            if (options.SubProtocols is [])
            {
                options.SubProtocols.Add("mqtt");
            }

            BindConfiguration(options, configuration);

            if (!string.IsNullOrEmpty(name))
            {
                BindConfiguration(options, configuration.GetSection(name));
            }

            static void BindConfiguration(WebSocketConnectionOptions options, IConfiguration config)
            {
                if (config.GetSection(nameof(WebSocketConnectionOptions.AllowedOrigins)).Exists())
                {
                    options.AllowedOrigins.Clear();
                }

                if (config.GetSection(nameof(WebSocketConnectionOptions.SubProtocols)).Exists())
                {
                    options.SubProtocols.Clear();
                }

                config.Bind(options);
            }
        }

        public void Configure(WebSocketConnectionOptions options) => Configure(Options.DefaultName, options);
    }

    private sealed class EndpointRouteBuilderProxy<TState>(IEndpointRouteBuilder endpoints,
        Action<IApplicationBuilder, TState> configure, TState state) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider => endpoints.ServiceProvider;

        public ICollection<EndpointDataSource> DataSources => endpoints.DataSources;

        public IApplicationBuilder CreateApplicationBuilder()
        {
            var applicationBuilder = endpoints.CreateApplicationBuilder();
            configure(applicationBuilder, state);
            return applicationBuilder;
        }
    }
}