using System.Net.Connections;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Logging.LogLevel;
using Listener = System.Collections.Generic.IAsyncEnumerable<System.Net.Connections.INetworkConnection>;

namespace System.Net.Mqtt.Server
{
    public static partial class MqttServerLoggingExtensions
    {
        [LoggerMessage(Level = Error, EventId = 1, EventName = "GeneralError",
            Message = "General unexpected error")]
        public static partial void LogGeneralError(this ILogger logger, Exception exception);

        [LoggerMessage(Level = Error, EventId = 2, EventName = "SessionError",
            Message = "{connection}: Error running MQTT session on this connection")]
        public static partial void LogSessionError(this ILogger logger, Exception exception, INetworkConnection connection);

        [LoggerMessage(Level = Error, EventId = 3, EventName = "ReplacementError",
            Message = "{clientId}: Error closing connection for existing session")]
        public static partial void LogSessionReplacementError(this ILogger logger, Exception exception, string clientId);

        [LoggerMessage(Level = Warning, EventId = 4, EventName = "TerminatedForcibly",
            Message = "{session}: Session terminated forcibly (due to server shutdown)")]
        public static partial void LogSessionTerminatedForcibly(this ILogger logger, MqttServerSession session);

        [LoggerMessage(Level = Warning, EventId = 5, EventName = "AbortedByClient",
            Message = "{session}: Connection abnormally aborted by the client (no DISCONNECT packet has been sent)")]
        public static partial void LogConnectionAbortedByClient(this ILogger logger, MqttServerSession session);

        [LoggerMessage(Level = Warning, EventId = 6, EventName = "VersionMismatch",
            Message = "{transport}: Cannot establish session, client requested unsupported protocol version '{version}'")]
        public static partial void LogProtocolVersionMismatch(this ILogger logger, NetworkTransport transport, int version);

        [LoggerMessage(Level = Warning, EventId = 7, EventName = "ConnectMissing",
            Message = "{transport}: Cannot establish session, client didn't send well formed CONNECT packet")]
        public static partial void LogMissingConnectPacket(this ILogger logger, NetworkConnectionAdapterTransport transport);

        [LoggerMessage(Level = Warning, EventId = 8, EventName = "InvalidClientId",
            Message = "{transport}: Cannot establish session, client provided invalid clientId")]
        public static partial void LogInvalidClientId(this ILogger logger, NetworkConnectionAdapterTransport transport);

        [LoggerMessage(Level = Warning, EventId = 9, EventName = "AuthFailed",
            Message = "{transport}: Authentication failed")]
        public static partial void LogAuthenticationFailed(this ILogger logger, NetworkConnectionAdapterTransport transport);

        [LoggerMessage(Level = Information, EventId = 10, EventName = "ListenerRegistered",
            Message = "Registered new connection listener '{name}' ({listener})")]
        public static partial void LogListenerRegistered(this ILogger logger, string name, Listener listener);

        [LoggerMessage(Level = Information, EventId = 11, EventName = "ListenerReady",
            Message = "{listener}: Ready to accept incoming connections")]
        public static partial void LogAcceptionStarted(this ILogger logger, Listener listener);

        [LoggerMessage(Level = Information, EventId = 12, EventName = "ConnectionAccepted",
            Message = "{listener}: New network connection accepted '{connection}'")]
        public static partial void LogNetworkConnectionAccepted(this ILogger logger, Listener listener, INetworkConnection connection);

        [LoggerMessage(Level = Information, EventId = 13, EventName = "SessionStarting",
            Message = "{session}: Starting session")]
        public static partial void LogSessionStarting(this ILogger logger, MqttServerSession session);

        [LoggerMessage(Level = Information, EventId = 14, EventName = "SessionStarted",
            Message = "{session}: Session started and ready to process messages")]
        public static partial void LogSessionStarted(this ILogger logger, MqttServerSession session);

        [LoggerMessage(Level = Information, EventId = 15, EventName = "SessionTerminatedGracefully",
            Message = "{session}: Session terminated gracefully")]
        public static partial void LogSessionTerminatedGracefully(this ILogger logger, MqttServerSession session);

        [LoggerMessage(Level = Information, EventId = 16, EventName = "IncomingMessage",
            Message = "Incoming message from '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}")]
        public static partial void LogIncomingMessage(this ILogger logger, string clientId, string topic, int size, byte qos, bool retain);

        [LoggerMessage(Level = Information, EventId = 17, EventName = "OutgoingMessage",
             Message = "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}")]
        public static partial void LogOutgoingMessage(this ILogger logger, string clientId, string topic, int size, byte qos, bool retain);
    }
}