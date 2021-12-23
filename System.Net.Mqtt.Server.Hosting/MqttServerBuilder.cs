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

    public IMqttServer Build()
    {
        var logger = loggerFactory.CreateLogger<MqttServer>();

        int maxDop = Environment.ProcessorCount;

        var server = new MqttServer(logger, new MqttProtocolHub[]
        {
            new Protocol.V3.ProtocolHub(logger, authHandler, maxDop),
            new Protocol.V4.ProtocolHub(logger, authHandler, maxDop)
        }, new MqttServerOptions()
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