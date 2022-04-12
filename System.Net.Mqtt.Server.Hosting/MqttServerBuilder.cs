using System.Net.Mqtt.Server.Hosting.Configuration;
using System.Net.Mqtt.Server.Protocol.V3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting;

public class MqttServerBuilder : IMqttServerBuilder
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly IOptions<MqttServerBuilderOptions> builderOptions;
    private readonly ILoggerFactory loggerFactory;
    private readonly IServiceProvider serviceProvider;

    public MqttServerBuilder(IOptions<MqttServerBuilderOptions> builderOptions, IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory, IMqttAuthenticationHandler authHandler = null)
    {
        ArgumentNullException.ThrowIfNull(builderOptions);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.builderOptions = builderOptions;
        this.serviceProvider = serviceProvider;
        this.loggerFactory = loggerFactory;
        this.authHandler = authHandler;
    }

    private IEnumerable<MqttProtocolHub> CreateHubs(MqttServerBuilderOptions builderOptions, ILogger logger)
    {
        var protocol = builderOptions.ProtocolLevel;
        var maxInFlight = builderOptions.MaxInFlight;

        if ((protocol & ProtocolLevel.Mqtt3_1) == ProtocolLevel.Mqtt3_1)
            yield return new ProtocolHub(logger, authHandler, maxInFlight);
        if ((protocol & ProtocolLevel.Mqtt3_1_1) == ProtocolLevel.Mqtt3_1_1)
            yield return new Protocol.V4.ProtocolHub(logger, authHandler, maxInFlight);
    }

    public async ValueTask<IMqttServer> BuildAsync()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();
        var options = builderOptions.Value;
        var server = new MqttServer(logger, CreateHubs(options, logger), new()
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeout),
            DisconnectTimeout = TimeSpan.FromMilliseconds(options.DisconnectTimeout)
        });

        try
        {
            foreach (var (name, factory) in options.ListenerFactories)
            {
                var listener = factory(serviceProvider);

                try
                {
                    server.RegisterListener(name, listener);
                }
                catch
                {
                    if (listener is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (listener is IDisposable disposable)
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
}