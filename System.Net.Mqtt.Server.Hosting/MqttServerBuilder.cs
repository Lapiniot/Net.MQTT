using System.Collections.Generic;
using System.Linq;
using System.Net.Connections;
using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting
{
    public class MqttServerBuilder : IMqttServerBuilder
    {
        private readonly IAsyncEnumerable<INetworkConnection>[] listeners;
        private readonly MqttServerBuilderOptions options;
        private readonly ILoggerFactory loggerFactory;
        private readonly IMqttAuthenticationHandler authHandler;

        public MqttServerBuilder(IOptions<MqttServerBuilderOptions> options,
            ILoggerFactory loggerFactory, IMqttAuthenticationHandler authHandler = null,
            IEnumerable<IAsyncEnumerable<INetworkConnection>> listeners = null) :
            this(options?.Value, loggerFactory, authHandler, listeners)
        { }

        public MqttServerBuilder(MqttServerBuilderOptions options,
            ILoggerFactory loggerFactory, IMqttAuthenticationHandler authHandler = null,
            IEnumerable<IAsyncEnumerable<INetworkConnection>> listeners = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.authHandler = authHandler;
            this.listeners = listeners?.ToArray();
        }

        public IMqttServer Build()
        {
            var logger = loggerFactory.CreateLogger<MqttServer>();

            var server = new MqttServer(logger, new MqttProtocolHub[]
            {
                new Protocol.V3.ProtocolHub(logger, authHandler, options.ConnectTimeout),
                new Protocol.V4.ProtocolHub(logger, authHandler, options.ConnectTimeout)
            });

            if(options != null)
            {
                foreach(var (name, listener) in options.Listeners)
                {
                    server.RegisterListener(name, listener);
                }
            }

            if(listeners != null)
            {
                for(var i = 0; i < listeners.Length; i++)
                {
                    var listener = listeners[i];
                    var name = $"{listener.GetType().Name}.{i + 1}";
                    server.RegisterListener(name, listener);
                }
            }

            return server;
        }
    }
}