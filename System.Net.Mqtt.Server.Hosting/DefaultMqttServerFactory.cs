using System.Net.Listeners;
using System.Net.Mqtt.Server.Hosting.Configuration;
using System.Net.Mqtt.Server.Protocol.V3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting
{
    public class DefaultMqttServerFactory : IMqttServerFactory
    {
        public DefaultMqttServerFactory(ILoggerFactory loggerFactory, IOptions<MqttServiceOptions> options) :
            this(loggerFactory, options.Value) {}

        public DefaultMqttServerFactory(ILoggerFactory loggerFactory, MqttServiceOptions options)
        {
            Logger = loggerFactory.CreateLogger<DefaultMqttServerFactory>();
            LoggerFactory = loggerFactory;
            Options = options;
        }

        public ILogger Logger { get; }
        public ILoggerFactory LoggerFactory { get; }
        public MqttServiceOptions Options { get; }

        public MqttServer Create()
        {
            Logger.LogInformation("Configuring new instance of the MQTT server...");

            var logger = LoggerFactory.CreateLogger<MqttServer>();
            var server = new MqttServer(logger, new ProtocolHub(logger), new Protocol.V4.ProtocolHub(logger));

            foreach(var (name, url) in Options.Endpoints)
            {
                var listener = url switch
                {
                    { Scheme: "tcp" } => (AsyncConnectionListener)new TcpSocketListener(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)),
                    { Scheme: "http", Host: "0.0.0.0" } u => new WebSocketsListener(new[] { $"{u.Scheme}://+:{u.Port}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
                    { Scheme: "http" } u => new WebSocketsListener(new[] { $"{u.Scheme}://{u.Authority}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
                    { Scheme: "ws", Host: "0.0.0.0" } u => new WebSocketsListener(new[] { $"http://+:{u.Port}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
                    { Scheme: "ws" } u => new WebSocketsListener(new[] { $"http://{u.Authority}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
                    _ => throw new ArgumentException("Uri schema not supported.")
                };

                server.RegisterListener(name, listener);
                Logger.LogInformation($"Registered new connection listener '{name}' ({listener.GetType().FullName}) for uri {url}.");
            }

            foreach(var (name, listener) in Options.Listeners)
            {
                server.RegisterListener(name, listener);
                Logger.LogInformation($"Registered new connection listener '{name}' ({listener.GetType().FullName}).");
            }

            return server;
        }
    }
}