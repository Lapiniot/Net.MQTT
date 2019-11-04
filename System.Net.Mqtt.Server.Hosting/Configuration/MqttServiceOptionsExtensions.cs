using System.Net.Listeners;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public static class MqttServiceOptionsExtensions
    {
        public static MqttServerOptions WithEndpoint(this MqttServerOptions options, string name, Uri url)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));
            if(name == null) throw new ArgumentNullException(nameof(name));
            if(url == null) throw new ArgumentNullException(nameof(url));

            options.Endpoints.Add(name, url);
            return options;
        }

        public static MqttServerOptions WithTcpEndpoint(this MqttServerOptions options, string name, IPEndPoint ipEndPoint)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));
            if(name == null) throw new ArgumentNullException(nameof(name));
            if(ipEndPoint == null) throw new ArgumentNullException(nameof(ipEndPoint));

            options.Listeners.Add(name, new TcpSocketListener(ipEndPoint));
            return options;
        }

        public static MqttServerOptions WithWebSocketsEndpoint(this MqttServerOptions options, string name, string[] prefixes, params string[] subProtocols)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));
            if(name == null) throw new ArgumentNullException(nameof(name));
            if(prefixes == null) throw new ArgumentNullException(nameof(prefixes));

            options.Listeners.Add(name, new WebSocketListener(prefixes, subProtocols));
            return options;
        }
    }
}