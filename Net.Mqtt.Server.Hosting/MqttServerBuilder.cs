using Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

namespace Net.Mqtt.Server.Hosting;

public class MqttServerBuilder : IMqttServerBuilder
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly IMeterFactory meterFactory;
    private readonly IOptions<ServerOptions> options;
    private readonly ILoggerFactory loggerFactory;

    public MqttServerBuilder(
        IOptions<ServerOptions> options,
        ILoggerFactory loggerFactory,
        IMqttAuthenticationHandler authHandler = null,
        IMeterFactory meterFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.options = options;
        this.loggerFactory = loggerFactory;
        this.authHandler = authHandler;
        this.meterFactory = meterFactory;
    }

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();
        var options = this.options.Value;
        return new MqttServer(logger, new()
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeout),
            Protocols = (MqttProtocol)options.ProtocolLevel,
            MaxInFlight = options.MaxInFlight ?? (ushort)short.MaxValue,
            MaxReceive = options.MaxReceive ?? (ushort)short.MaxValue,
            MaxUnflushedBytes = options.MaxUnflushedBytes ?? int.MaxValue,
            MaxPacketSize = options.MaxPacketSize ?? int.MaxValue,
            AuthenticationHandler = authHandler,
            MQTT5 = new()
            {
                MaxInFlight = options.MQTT5?.MaxInFlight ?? options.MaxInFlight ?? (ushort)short.MaxValue,
                MaxReceive = options.MQTT5?.MaxReceive ?? options.MaxReceive ?? (ushort)short.MaxValue,
                MaxUnflushedBytes = options.MQTT5?.MaxUnflushedBytes ?? options.MaxUnflushedBytes ?? int.MaxValue,
                MaxPacketSize = options.MQTT5?.MaxPacketSize ?? options.MaxPacketSize ?? int.MaxValue
            }
        }, options.ListenerFactories, meterFactory);
    }
}