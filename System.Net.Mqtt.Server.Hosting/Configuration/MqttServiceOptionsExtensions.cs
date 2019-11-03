using System.Net.Listeners;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public static class MqttServiceOptionsExtensions
    {
        public static MqttServiceOptions WithEndpoint(this MqttServiceOptions options, string name, Uri url)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));
            if(name == null) throw new ArgumentNullException(nameof(name));
            if(url == null) throw new ArgumentNullException(nameof(url));

            options.Endpoints.Add(name, url);
            return options;
        }

        public static MqttServiceOptions WithTcpEndpoint(this MqttServiceOptions options, string name, IPEndPoint ipEndPoint)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));
            if(name == null) throw new ArgumentNullException(nameof(name));
            if(ipEndPoint == null) throw new ArgumentNullException(nameof(ipEndPoint));

            options.Listeners.Add(name, new TcpSocketListener(ipEndPoint));
            return options;
        }

        public static MqttServiceOptions WithWebSocketsEndpoint(this MqttServiceOptions options, string name, Uri url, params string[] subProtocols)
        {
            if(options == null) throw new ArgumentNullException(nameof(options));
            if(name == null) throw new ArgumentNullException(nameof(name));
            if(url == null) throw new ArgumentNullException(nameof(url));

            options.Listeners.Add(name, new WebSocketsListener(new[] {url.AbsoluteUri}, subProtocols));
            return options;
        }
    }
}