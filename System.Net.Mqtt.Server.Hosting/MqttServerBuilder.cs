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
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        this.authHandler = authHandler;
    }

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();

        var server = new MqttServer(logger, new MqttProtocolHub[]
        {
                new Protocol.V3.ProtocolHub(logger, authHandler, options.ConnectTimeout),
                new Protocol.V4.ProtocolHub(logger, authHandler, options.ConnectTimeout)
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
                    (listener as IDisposable)?.Dispose();
                    throw;
                }
            }

            return server;
        }
        catch
        {
            (server as IDisposable)?.Dispose();
            throw;
        }
    }
}