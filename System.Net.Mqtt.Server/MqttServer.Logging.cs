using System.Net.Connections;
using static Microsoft.Extensions.Logging.LogLevel;
using Listener = System.Collections.Generic.IAsyncEnumerable<System.Net.Connections.INetworkConnection>;

namespace System.Net.Mqtt.Server;

public partial class MqttServer
{
    private readonly ILogger<MqttServer> logger;

    [LoggerMessage(1, Error, "General unexpected error", EventName = "GeneralError")]
    private partial void LogGeneralError(Exception exception);

    [LoggerMessage(2, Error, "{connection}: Error running MQTT session on this connection", EventName = "SessionError")]
    private partial void LogSessionError(Exception exception, INetworkConnection connection);

    [LoggerMessage(3, Error, "{clientId}: Error closing connection for existing session", EventName = "ReplacementError")]
    private partial void LogSessionReplacementError(Exception exception, string clientId);

    [LoggerMessage(4, Warning, "{session}: Session terminated forcibly (due to server shutdown)", EventName = "TerminatedForcibly")]
    private partial void LogSessionTerminatedForcibly(MqttServerSession session);

    [LoggerMessage(5, Warning, "{session}: Connection abnormally aborted by the client (no DISCONNECT sent)", EventName = "AbortedByClient")]
    private partial void LogConnectionAbortedByClient(MqttServerSession session);

    [LoggerMessage(6, Warning, "{transport}: Cannot establish session, client requested unsupported protocol version '{version}'", EventName = "VersionMismatch")]
    private partial void LogProtocolVersionMismatch(NetworkTransport transport, int version);

    [LoggerMessage(7, Warning, "{transport}: Cannot establish session, client didn't send well formed CONNECT packet", EventName = "ConnectMissing")]
    private partial void LogMissingConnectPacket(NetworkTransport transport);

    [LoggerMessage(8, Warning, "{transport}: Cannot establish session, client provided invalid clientId", EventName = "InvalidClientId")]
    private partial void LogInvalidClientId(NetworkTransport transport);

    [LoggerMessage(9, Warning, "{transport}: Authentication failed", EventName = "AuthFailed")]
    private partial void LogAuthenticationFailed(NetworkTransport transport);

    [LoggerMessage(10, Information, "Registered new connection listener '{name}' ({listener})", EventName = "ListenerRegistered")]
    private partial void LogListenerRegistered(string name, Listener listener);

    [LoggerMessage(11, Information, "{listener}: Ready to accept incoming connections", EventName = "ListenerReady")]
    private partial void LogAcceptionStarted(Listener listener);

    [LoggerMessage(12, Information, "{listener}: New network connection accepted '{connection}'", EventName = "ConnectionAccepted")]
    private partial void LogNetworkConnectionAccepted(Listener listener, INetworkConnection connection);

    [LoggerMessage(13, Information, "{session}: Starting session", EventName = "SessionStarting")]
    private partial void LogSessionStarting(MqttServerSession session);

    [LoggerMessage(14, Information, "{session}: Session started and ready to process messages", EventName = "SessionStarted")]
    private partial void LogSessionStarted(MqttServerSession session);

    [LoggerMessage(15, Information, "{session}: Session terminated gracefully (DISCONNECT sent)", EventName = "SessionTerminatedGracefully")]
    private partial void LogSessionTerminatedGracefully(MqttServerSession session);

    [LoggerMessage(16, Debug, "Incoming message from '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "IncomingMessage", SkipEnabledCheck = true)]
    private partial void LogIncomingMessage(string clientId, string topic, int size, byte qos, bool retain);
}