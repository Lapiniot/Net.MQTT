using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting;

public class MqttServerBuilder : IMqttServerBuilder
{
    private readonly MqttServerBuilderOptions options;
    private readonly IServiceProvider serviceProvider;
    private readonly ILoggerFactory loggerFactory;
    private readonly IMqttAuthenticationHandler authHandler;

    public MqttServerBuilder(MqttServerBuilderOptions options, IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory, IMqttAuthenticationHandler authHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.options = options;
        this.serviceProvider = serviceProvider;
        this.loggerFactory = loggerFactory;
        this.authHandler = authHandler;
    }

    public async ValueTask<IMqttServer> BuildAsync()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();

        var server = new MqttServer(logger, CreateHubs(options, logger), new MqttServerOptions
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeout),
            DisconnectTimeout = TimeSpan.FromMilliseconds(options.DisconnectTimeout)
        });

        try
        {
            foreach(var (name, factory) in options.ListenerFactories)
            {
                var listener = factory(serviceProvider);

                try
                {
                    server.RegisterListener(name, listener);
                }
                catch
                {
                    if(listener is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else if(listener is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    throw;
                }
            }

            return server;
        }
        catch
        {
            await server.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private IEnumerable<MqttProtocolHub> CreateHubs(MqttServerBuilderOptions builderOptions, ILogger logger)
    {
        var protocol = builderOptions.ProtocolLevel;
        var maxPublishInFlight = builderOptions.MaxPublishInFlight;

        if((protocol & ProtocolLevel.Mqtt3_1) == ProtocolLevel.Mqtt3_1)
            yield return new Protocol.V3.ProtocolHub(logger, authHandler, maxPublishInFlight);
        if((protocol & ProtocolLevel.Mqtt3_1_1) == ProtocolLevel.Mqtt3_1_1)
            yield return new Protocol.V4.ProtocolHub(logger, authHandler, maxPublishInFlight);
    }
}