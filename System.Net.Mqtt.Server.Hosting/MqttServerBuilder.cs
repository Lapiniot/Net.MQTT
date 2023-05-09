using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting;

public class MqttServerBuilder : IMqttServerBuilder
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly IOptions<Configuration.MqttServerOptions> builderOptions;
    private readonly ILoggerFactory loggerFactory;

    public MqttServerBuilder(IOptions<Configuration.MqttServerOptions> builderOptions,
        ILoggerFactory loggerFactory, IMqttAuthenticationHandler authHandler = null)
    {
        ArgumentNullException.ThrowIfNull(builderOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.builderOptions = builderOptions;
        this.loggerFactory = loggerFactory;
        this.authHandler = authHandler;
    }

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();
        var options = builderOptions.Value;
        return new MqttServer(logger, new()
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeout),
            MaxInFlight = options.MaxInFlight,
            MaxUnflushedBytes = options.MaxUnflushedBytes,
            Level = (ProtocolLevel)options.ProtocolLevel,
            AuthenticationHandler = authHandler
        }, options.ListenerFactories.AsReadOnly());
    }
}