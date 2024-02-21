using Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

namespace Net.Mqtt.Server.Hosting;

public class MqttServerBuilder : IMqttServerBuilder
{
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly IMeterFactory meterFactory;
    private readonly ICertificateValidationPolicy validationPolicy;
    private readonly IHostEnvironment environment;
    private readonly IOptions<ServerOptions> options;
    private readonly ILoggerFactory loggerFactory;

    public MqttServerBuilder(
        IHostEnvironment environment,
        IOptions<ServerOptions> options,
        ILoggerFactory loggerFactory,
        IMqttAuthenticationHandler authHandler = null,
        IMeterFactory meterFactory = null,
        ICertificateValidationPolicy validationPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(environment);

        this.environment = environment;
        this.options = options;
        this.loggerFactory = loggerFactory;
        this.authHandler = authHandler;
        this.meterFactory = meterFactory;
        this.validationPolicy = validationPolicy;
    }

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();
        var options = this.options.Value;
        return new MqttServer(logger, options.Map() with { AuthenticationHandler = authHandler },
            options.Endpoints.Map(environment, validationPolicy), meterFactory);
    }
}