using System.Collections.Generic;
using System.Linq;
using System.Net.Connections;
using System.Net.Listeners;
using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting
{
    public class MqttServerBuilder : IMqttServerBuilder
    {
        private readonly IAsyncEnumerable<INetworkConnection>[] listeners;
        private readonly MqttServerOptions options;
        private readonly ILoggerFactory loggerFactory;
        private string[] subProtocols;

        public MqttServerBuilder(IOptions<MqttServerOptions> options, ILoggerFactory loggerFactory,
            IEnumerable<IAsyncEnumerable<INetworkConnection>> listeners = null) :
            this(options?.Value, loggerFactory, listeners)
        { }

        public MqttServerBuilder(MqttServerOptions options, ILoggerFactory loggerFactory,
            IEnumerable<IAsyncEnumerable<INetworkConnection>> listeners = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.listeners = listeners?.ToArray();
        }

        public MqttServer Build()
        {
            var logger = loggerFactory.CreateLogger<MqttServerBuilder>();
            logger.LogInformation("Configuring new instance of the MQTT server...");

            var server = new MqttServer(loggerFactory.CreateLogger<MqttServer>(), new MqttProtocolHub[]
            {
                new Protocol.V3.ProtocolHub(logger),
                new Protocol.V4.ProtocolHub(logger)
            });

            if(options != null)
            {
                foreach(var (name, url) in options.Endpoints)
                {
                    var listener = CreateListener(url);
                    server.RegisterListener(name, listener);

                }

                foreach(var (name, listener) in options.Listeners)
                {
                    server.RegisterListener(name, listener);
                    logger.LogInformation($"Registered new connection listener '{name}' ({listener}).");
                }
            }

            if(listeners != null)
            {
                for(var i = 0; i < listeners.Length; i++)
                {
                    var listener = listeners[i];
                    var name = $"{listener.GetType().Name}.{i + 1}";
                    server.RegisterListener(name, listener);
                    logger.LogInformation($"Registered new connection listener '{name}' ({listener}).");
                }
            }

            return server;
        }

        private string[] GetSubProtocols()
        {
            return subProtocols ??= new string[] { "mqtt", "mqttv3.1" };
        }

        private IAsyncEnumerable<INetworkConnection> CreateListener(Uri url)
        {
            return url switch
            {
                { Scheme: "tcp" } => new TcpSocketListener(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)),
                { Scheme: "http", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"{u.Scheme}://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "http" } u => new WebSocketListener(new[] { $"{u.Scheme}://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "ws", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"http://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "ws" } u => new WebSocketListener(new[] { $"http://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
                _ => throw new ArgumentException("Uri schema not supported.")
            };
        }
    }
}