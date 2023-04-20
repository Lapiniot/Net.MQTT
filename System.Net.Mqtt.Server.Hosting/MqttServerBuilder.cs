using System.Net.Mqtt.Server.Hosting.Configuration;
using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Mqtt.Server.Protocol.V5;
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

    private IEnumerable<MqttProtocolHub> CreateHubs(Configuration.MqttServerOptions options, ILogger logger)
    {
        var protocol = options.ProtocolLevel;
        var maxInFlight = options.MaxInFlight;
        var maxUnflushedBytes = options.MaxUnflushedBytes;
        var connectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeout);

        if ((protocol & ProtocolLevel.Mqtt3_1) == ProtocolLevel.Mqtt3_1)
            yield return new ProtocolHub3(logger, authHandler, maxInFlight, maxUnflushedBytes, connectTimeout);
        if ((protocol & ProtocolLevel.Mqtt3_1_1) == ProtocolLevel.Mqtt3_1_1)
            yield return new ProtocolHub4(logger, authHandler, maxInFlight, maxUnflushedBytes, connectTimeout);
#if DEBUG
        if ((protocol & ProtocolLevel.Mqtt5) == ProtocolLevel.Mqtt5)
            yield return new ProtocolHub5(logger, authHandler, maxInFlight, maxUnflushedBytes, connectTimeout);
#endif
    }

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();
        var options = builderOptions.Value;
        return new MqttServer(logger, new() { ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeout) },
            options.ListenerFactories.AsReadOnly(), CreateHubs(options, logger));
    }
}