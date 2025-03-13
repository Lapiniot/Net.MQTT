using static Microsoft.Extensions.Logging.LogLevel;
using Listener = System.Collections.Generic.IAsyncEnumerable<OOs.Net.Connections.TransportConnection>;

namespace Net.Mqtt.Server;

internal static partial class MqttServerLoggingExtensions
{
    [LoggerMessage(1, Error, "General unexpected error", EventName = "GeneralError")]
    public static partial void LogGeneralError(this ILogger logger, Exception exception);

    [LoggerMessage(2, Error, "{connection}: Error running MQTT session on this connection", EventName = "SessionError")]
    public static partial void LogSessionError(this ILogger logger, Exception exception, TransportConnection connection);

    [LoggerMessage(3, Error, "{clientId}: Error closing connection for existing session", EventName = "TakeoverError")]
    public static partial void LogSessionTakeoverError(this ILogger logger, Exception exception, string clientId);

    [LoggerMessage(4, Warning, "{session}: Session has been forcibly aborted by the server (reason: {reason})", EventName = "TerminatedByServer")]
    public static partial void LogSessionAbortedForcibly(this ILogger logger, MqttServerSession session, DisconnectReason reason);

    [LoggerMessage(20, Warning, "{session}: Session has been terminated by the client (reason: {reason})", EventName = "TerminatedByClient")]
    public static partial void LogSessionAbortedByClient(this ILogger logger, MqttServerSession session, DisconnectReason reason);

    [LoggerMessage(5, Warning, "{session}: Connection abnormally aborted by the client (no DISCONNECT sent)", EventName = "AbortedByClient")]
    public static partial void LogConnectionAbortedByClient(this ILogger logger, MqttServerSession session);

    [LoggerMessage(6, Warning, "{connection}: Cannot establish session, client requested unsupported protocol version '{version}'", EventName = "VersionMismatch")]
    public static partial void LogProtocolVersionMismatch(this ILogger logger, TransportConnection connection, int version);

    [LoggerMessage(7, Warning, "{connection}: Cannot establish session, client didn't send well formed CONNECT packet", EventName = "ConnectMissing")]
    public static partial void LogMissingConnectPacket(this ILogger logger, TransportConnection connection);

    [LoggerMessage(8, Warning, "{connection}: Cannot establish session, client provided invalid clientId", EventName = "InvalidClientId")]
    public static partial void LogInvalidClientId(this ILogger logger, TransportConnection connection);

    [LoggerMessage(9, Warning, "{connection}: Authentication failed", EventName = "AuthFailed")]
    public static partial void LogAuthenticationFailed(this ILogger logger, TransportConnection connection);

    [LoggerMessage(10, Information, "Registered new connection listener '{name}' ({listener})", EventName = "ListenerRegistered")]
    public static partial void LogListenerRegistered(this ILogger logger, string name, Listener listener);

    [LoggerMessage(21, Error, "Failed to register new connection listener '{name}'", EventName = "ListenerRegistrationError")]
    public static partial void LogListenerRegistrationError(this ILogger logger, string name, Exception exception);

    [LoggerMessage(11, Information, "{listener}: Ready to accept incoming connections", EventName = "ListenerReady")]
    public static partial void LogAcceptionStarted(this ILogger logger, Listener listener);

    [LoggerMessage(22, Error, "{listener}: stopped listening due to the error", EventName = "ListenerError")]
    public static partial void LogListenerError(this ILogger logger, Listener listener, Exception exception);

    [LoggerMessage(12, Information, "{listener}: New network connection accepted '{connection}'", EventName = "ConnectionAccepted")]
    public static partial void LogNetworkConnectionAccepted(this ILogger logger, Listener listener, TransportConnection connection);

    [LoggerMessage(13, Information, "{session}: Starting session", EventName = "SessionStarting")]
    public static partial void LogSessionStarting(this ILogger logger, MqttServerSession session);

    [LoggerMessage(14, Information, "{session}: Session started and ready to process messages", EventName = "SessionStarted")]
    public static partial void LogSessionStarted(this ILogger logger, MqttServerSession session);

    [LoggerMessage(15, Information, "{session}: Session terminated gracefully (DISCONNECT sent)", EventName = "SessionTerminatedGracefully")]
    public static partial void LogSessionTerminatedGracefully(this ILogger logger, MqttServerSession session);

    [LoggerMessage(16, Debug, "Incoming message from '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "IncomingMessage", SkipEnabledCheck = true)]
    public static partial void LogIncomingMessage(this ILogger logger, string clientId, string topic, int size, int qos, bool retain);

    [LoggerMessage(17, Debug, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage", SkipEnabledCheck = true)]
    public static partial void LogOutgoingMessage(this ILogger logger, string clientId, string topic, int size, int qos, bool retain);

    [LoggerMessage(18, Information, "Registered diagnostic meter '{name}'")]
    public static partial void LogMeterRegistered(this ILogger logger, string name);

    [LoggerMessage(19, Warning, "'{connection}' timed out. Client didn't send CONNECT packet within a reasonable amount of time.", EventName = "ConnectTimeout")]
    public static partial void LogConnectTimeout(this ILogger logger, TransportConnection connection);
}