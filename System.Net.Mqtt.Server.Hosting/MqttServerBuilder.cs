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

    public MqttServerBuilder(IOptions<MqttServerBuilderOptions> builderOptions,
        ILoggerFactory loggerFactory, IMqttAuthenticationHandler authHandler = null)
    {
        ArgumentNullException.ThrowIfNull(builderOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.builderOptions = builderOptions;
        this.loggerFactory = loggerFactory;
        this.authHandler = authHandler;
    }

    private IEnumerable<MqttProtocolHub> CreateHubs(MqttServerBuilderOptions options, ILogger logger)
    {
        var protocol = options.ProtocolLevel;
        var maxInFlight = options.MaxInFlight;
        var maxUnflushedBytes = options.MaxUnflushedBytes;

        if ((protocol & ProtocolLevel.Mqtt3_1) == ProtocolLevel.Mqtt3_1)
            yield return new ProtocolHub(logger, authHandler, maxInFlight, maxUnflushedBytes);
        if ((protocol & ProtocolLevel.Mqtt3_1_1) == ProtocolLevel.Mqtt3_1_1)
            yield return new Protocol.V4.ProtocolHub(logger, authHandler, maxInFlight, maxUnflushedBytes);
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
                var listener = factory();

                try
                {
                    server.RegisterListener(name, listener);
                }
                catch
                {
                    if (listener is IDisposable disposable)
                        disposable.Dispose();

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