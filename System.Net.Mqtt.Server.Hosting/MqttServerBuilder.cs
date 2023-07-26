using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting;

public class MqttServerBuilder : IMqttServerBuilder
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly IOptions<ServerOptions> options;
    private readonly ILoggerFactory loggerFactory;

    public MqttServerBuilder(IOptions<ServerOptions> options,
        ILoggerFactory loggerFactory, IMqttAuthenticationHandler authHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.options = options;
        this.loggerFactory = loggerFactory;
        this.authHandler = authHandler;
    }

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();
        var options = this.options.Value;
        return new MqttServer(logger, new()
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeout),
            MaxInFlight = options.MaxInFlight,
            MaxReceive5 = options.MaxReceive5,
            MaxUnflushedBytes = options.MaxUnflushedBytes,
            Protocols = (MqttProtocols)options.ProtocolLevel,
            AuthenticationHandler = authHandler
        }, options.ListenerFactories.AsReadOnly());
    }
}