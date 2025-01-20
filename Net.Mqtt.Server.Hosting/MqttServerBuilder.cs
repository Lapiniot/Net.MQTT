using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

namespace Net.Mqtt.Server.Hosting;

public sealed class MqttServerBuilder : IMqttServerBuilder
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly IMeterFactory meterFactory;
    private readonly IOptions<MqttServerOptions> options;
    private readonly ILoggerFactory loggerFactory;
    private readonly IServiceProvider serviceProvider;

    public MqttServerBuilder(
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider,
        IOptions<MqttServerOptions> options,
        IMqttAuthenticationHandler authHandler = null,
        IMeterFactory meterFactory = null)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);

        this.loggerFactory = loggerFactory;
        this.serviceProvider = serviceProvider;
        this.options = options;
        this.authHandler = authHandler;
        this.meterFactory = meterFactory;
    }

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();
        var options = this.options.Value;
        return new MqttServer(logger, options.Map(), options.Endpoints.Map(serviceProvider), meterFactory, authHandler);
    }
}