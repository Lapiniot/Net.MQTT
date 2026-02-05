using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Net.Mqtt.Server.Hosting;
using static Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

#pragma warning disable CA1708 // Identifiers should differ by more than case
#pragma warning disable CA1034 // Nested types should not be visible

public static class HttpServerIntegrationExtensions
{
    private const string ConfigSectionPath = "KestrelMQTT";

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps incoming requests with the specified <paramref name="pattern"/> to the provided connection 
        /// pipeline built by <paramref name="endpoints"/>.
        /// </summary>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder" /> that can be used to further customize the endpoint.</returns>
        public IEndpointConventionBuilder MapMqttWebSockets(string? pattern = null)
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
    }

    extension(IConnectionBuilder connectionBuilder)
    {
        /// <summary>
        /// Use connections from this endpoint for MQTT server bridge listener (enables integration 
        /// between Kestrel and MQTT server on this connection pipeline).
        /// </summary>
        /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
        public IConnectionBuilder UseMqttServer()
        {
            ArgumentNullException.ThrowIfNull(connectionBuilder);

            var handler = ActivatorUtilities.GetServiceOrCreateInstance<HttpServerBridgeConnectionHandler>(connectionBuilder.ApplicationServices);
            connectionBuilder.Run(handler.OnConnectedAsync);

            if (connectionBuilder is IMultiplexedConnectionBuilder multiplexedConnectionBuilder)
            {
                multiplexedConnectionBuilder.Use(next => (context) => handler.OnConnectedAsync(context));
            }

            return connectionBuilder;
        }
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="WebSocketBridgeConnectionHandler"/> and related services in the DI container.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed</returns>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ConnectionQueueListenerOptions))]
        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public IServiceCollection AddWebSocketsHandler()
        {
            services.AddOptionsWithValidateOnStart<WebSocketConnectionOptions, WebSocketConnectionOptionsValidator>();
            services.TryAddEnumerable(
                Transient<IConfigureOptions<WebSocketConnectionOptions>, WebSocketConnectionOptionsSetup>(
                    static sp => new(sp.GetRequiredService<IConfiguration>().GetSection($"{ConfigSectionPath}:WebSockets"))));
            services.TryAddSingleton<WebSocketBridgeConnectionHandler>();
            return services;
        }

        /// <summary>
        /// Registers all required 'glue layer' services to enable HTTP server and MQTT server integration.
        /// </summary>
        /// <param name="endPointName">The MQTT listener endpoint name.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        internal IServiceCollection AddConnectionQueueListener(string? endPointName = null)
        {
            services.AddOptions<MqttServerOptions>().Configure<ConnectionQueueListener>(ConfigureOptions);
            services
                .AddOptionsWithValidateOnStart<ConnectionQueueListenerOptions, ConnectionQueueListenerOptionsValidator>()
                .BindConfiguration(ConfigSectionPath);

            services.TryAddSingleton<ConnectionQueueListener>();
            services.TryAddSingleton<ITransportConnectionHandler>(
                static serviceProvider => serviceProvider.GetRequiredService<ConnectionQueueListener>());

            void ConfigureOptions(MqttServerOptions options, ConnectionQueueListener listener) =>
                options.Endpoints[endPointName ?? "aspnet.connections"] = new(() => listener);

            return services;
        }
    }

    extension(MqttServerOptionsBuilder builder)
    {
        /// <summary>
        /// Registers web-sockets listener adapter, which serves as a 'glue layer' between
        /// web-socket interceptor middleware and MQTT server connection listener infrastructure.
        /// </summary>
        /// <param name="endPointName">Endpoint display name.</param>
        /// <returns>The <see cref="MqttServerOptionsBuilder"/>.</returns>
        public MqttServerOptionsBuilder UseHttpServerWebSocketConnections(string? endPointName = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.AddWebSocketsHandler();
            builder.UseHttpServer(endPointName);
            return builder;
        }

        /// <summary>
        /// Registers all required 'glue layer' services to enable HTTP server integration.
        /// </summary>
        /// <param name="endPointName">The MQTT listener endpoint name.</param>
        /// <returns>The <see cref="MqttServerOptionsBuilder"/>.</returns>
        public MqttServerOptionsBuilder UseHttpServer(string? endPointName = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.AddConnectionQueueListener(endPointName);
            return builder;
        }
    }

    extension(IWebHostBuilder builder)
    {
        /// <summary>
        /// Registers all required 'glue layer' services to enable MQTT server integration.
        /// </summary>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseMqttCore()
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.ConfigureServices(static services => services.AddConnectionQueueListener());
        }

        /// <summary>
        /// Registers all required 'glue layer' services to enable MQTT server integration + dynamically applies 
        /// MQTT transport handlers to the corresponding Kestrel's connection endpoints mapping based on the configuration.
        /// </summary>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseMqtt()
        {
            builder.UseMqttCore();
            return builder.ConfigureServices(static services => services.TryAddEnumerable(
                Transient<IPostConfigureOptions<KestrelServerOptions>, KestrelServerOptionsPostSetup>(
                    static sp => new(sp.GetRequiredService<IConfiguration>().GetSection($"{ConfigSectionPath}:UseEndpoints")))));
        }

        /// <summary>
        /// Registers all required 'glue layer' services to enable MQTT server integration + dynamically applies 
        /// MQTT transport handlers to the corresponding Kestrel's connection endpoints mapping based on the configuration.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to read configuration defaults from.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseMqtt(IConfiguration configuration)
        {
            builder.UseMqttCore();
            return builder.ConfigureServices(services => services.TryAddEnumerable(
                Transient<IPostConfigureOptions<KestrelServerOptions>, KestrelServerOptionsPostSetup>(
                    sp => new(configuration))));
        }
    }
}