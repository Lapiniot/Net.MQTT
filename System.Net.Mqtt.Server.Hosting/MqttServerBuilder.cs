using System.Net.Listeners;
using System.Net.Mqtt.Server.Hosting.Configuration;
using System.Net.Mqtt.Server.Protocol.V3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting
{
    public class MqttServerBuilder : IMqttServerBuilder
    {
        private readonly IConnectionListener[] listeners;

        public MqttServerBuilder(ILoggerFactory loggerFactory, IOptions<MqttServerOptions> options, IConnectionListener[] listeners = default) :
            this(loggerFactory, options.Value, listeners)
        { }

        public MqttServerBuilder(ILoggerFactory loggerFactory, MqttServerOptions options, IConnectionListener[] listeners)
        {
            Logger = loggerFactory.CreateLogger<MqttServerBuilder>();
            LoggerFactory = loggerFactory;
            Options = options;
            this.listeners = listeners;
        }

        public ILogger Logger { get; }
        public ILoggerFactory LoggerFactory { get; }
        public MqttServerOptions Options { get; }

        public MqttServer Build()
        {
            Logger.LogInformation("Configuring new instance of the MQTT server...");

            var logger = LoggerFactory.CreateLogger<MqttServer>();
            var server = new MqttServer(logger, new ProtocolHub(logger), new Protocol.V4.ProtocolHub(logger));

            foreach(var (name, url) in Options.Endpoints)
            {
                var listener = url switch
                {
                    { Scheme: "tcp" } => (ConnectionListener)new TcpSocketListener(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)),
                    { Scheme: "http", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"{u.Scheme}://+:{u.Port}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
                    { Scheme: "http" } u => new WebSocketListener(new[] { $"{u.Scheme}://{u.Authority}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
                    { Scheme: "ws", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"http://+:{u.Port}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
                    { Scheme: "ws" } u => new WebSocketListener(new[] { $"http://{u.Authority}{u.PathAndQuery}" }, "mqtt", "mqttv3.1"),
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

            if(listeners != null)
            {
                for(int i = 0; i < listeners.Length; i++)
                {
                    var listener = listeners[i];
                    string name = $"{listener.GetType().Name}.{i}";
                    server.RegisterListener(name, listener);
                    Logger.LogInformation($"Registered new connection listener '{name}' ({listener.GetType().FullName}).");
                }
            }

            return server;
        }
    }
}