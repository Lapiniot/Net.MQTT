using System.Collections.Generic;
using System.Net.Connections;
using System.Net.Listeners;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public static class MqttServerBuilderOptionsExtensions
    {
        private static string[] subProtocols;

        public static MqttServerBuilderOptions UseEndpoint(this MqttServerBuilderOptions options, string name, Uri uri)
        {
            if(options is null) throw new ArgumentNullException(nameof(options));

            options.Listeners.Add(name, CreateListener(uri));
            
            return options;
        }

        private static IAsyncEnumerable<INetworkConnection> CreateListener(Uri uri)
        {
            return uri switch
            {
                { Scheme: "tcp" } => new TcpSocketListener(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port)),
                { Scheme: "http", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"{u.Scheme}://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "http" } u => new WebSocketListener(new[] { $"{u.Scheme}://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "ws", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"http://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "ws" } u => new WebSocketListener(new[] { $"http://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
                _ => throw new ArgumentException("Uri schema not supported.")
            };
        }

        private static string[] GetSubProtocols()
        {
            return subProtocols ??= new string[] { "mqtt", "mqttv3.1" };
        }
    }
}