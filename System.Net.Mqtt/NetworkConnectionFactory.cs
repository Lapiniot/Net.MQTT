using System.Diagnostics.CodeAnalysis;
using System.Net.Connections;

namespace System.Net.Mqtt
{
    [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope",
        Justification = "NetworkConnectionAdapterTransport takes ownership on INetworkConnection and disposes it")]
    public static class NetworkTransportFactory
    {
        public static NetworkTransport Create(Uri uri)
        {
            return uri switch
            {
                null => throw new ArgumentNullException(nameof(uri)),
                { Scheme: "ws" or "wss" } u => CreateWebSockets(u),
                { Scheme: "http" } u => CreateWebSockets(new UriBuilder(u) { Scheme = "ws" }.Uri),
                { Scheme: "https" } u => CreateWebSockets(new UriBuilder(u) { Scheme = "wss" }.Uri),
                { Scheme: "tcp", Host: var host, Port: var port } => CreateTcp(host, port),
                _ => throw new ArgumentException("Protocol schema is not supported")
            };
        }

        public static NetworkTransport CreateWebSockets(Uri uri, string[] subProtocols)
        {
            return new NetworkConnectionAdapterTransport(
                new WebSocketClientConnection(uri, subProtocols),
                true);
        }

        public static NetworkTransport CreateWebSockets(Uri uri)
        {
            return new NetworkConnectionAdapterTransport(
                new WebSocketClientConnection(uri, new string[] { "mqttv3.1", "mqtt" }),
                true);
        }

        public static NetworkTransport CreateTcp(IPEndPoint endPoint)
        {
            return new NetworkConnectionAdapterTransport(
                new TcpSocketClientConnection(endPoint),
                true);
        }

        public static NetworkTransport CreateTcp(IPAddress address, int port)
        {
            return new NetworkConnectionAdapterTransport(
                new TcpSocketClientConnection(new IPEndPoint(address, port)),
                true);
        }

        public static NetworkTransport CreateTcp(string hostNameOrAddress, int port)
        {
            return new NetworkConnectionAdapterTransport(
                new TcpSocketClientConnection(hostNameOrAddress, port),
                true);
        }
    }
}