using System.Net.Connections;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt;

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
            { Scheme: "tcps", Host: var host, Port: var port } => CreateTcpSsl(host, port),
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

    public static NetworkTransport CreateTcpSsl(IPEndPoint endPoint,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate2 certificate = null)
    {
        return new NetworkConnectionAdapterTransport(
            new SslStreamClientConnection(endPoint, machineName, enabledSslProtocols, certificate),
            true);
    }

    public static NetworkTransport CreateTcpSsl(IPAddress address, int port,
        string machineName, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate2 certificate = null)
    {
        return new NetworkConnectionAdapterTransport(
            new SslStreamClientConnection(new IPEndPoint(address, port), machineName, enabledSslProtocols, certificate),
            true);
    }

    public static NetworkTransport CreateTcpSsl(string hostNameOrAddress, int port,
        string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate2 certificate = null)
    {
        return new NetworkConnectionAdapterTransport(
            new SslStreamClientConnection(hostNameOrAddress, port, machineName, enabledSslProtocols, certificate),
            true);
    }
}