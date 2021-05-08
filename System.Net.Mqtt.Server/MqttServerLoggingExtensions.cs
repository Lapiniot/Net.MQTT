using System.Net.Connections;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Logging.LogLevel;
using Listener = System.Collections.Generic.IAsyncEnumerable<System.Net.Connections.INetworkConnection>;

namespace System.Net.Mqtt.Server
{
    internal static class MqttServerLoggingExtensions
    {
        private static Action<ILogger, Listener, Exception> logAcceptionStarted = LoggerMessage.Define<Listener>(
            Information, new EventId(1, "StartAccepting"), "Start accepting incoming connections for {listener}");
        private static Action<ILogger, Listener, INetworkConnection, Exception> logConnectionAccepted = LoggerMessage.Define<Listener, INetworkConnection>(
            Information, new EventId(2, "ConnectionAccepted"), "Network connection accepted by {listener} <=> {connection}");
        private static Action<ILogger, INetworkConnection, Exception> logSessionRejectedError = LoggerMessage.Define<INetworkConnection>(
            Error, new EventId(3, "SessionRejectedError"), "Cannot establish MQTT session for connection {connection}");
        private static Action<ILogger, string, Exception> logSessionReplacementError = LoggerMessage.Define<string>(
            Error, new EventId(4, "SessionReplacementError"), "Error closing connection for existing session '{clientId}'");
        private static Action<ILogger, string, Exception> logConnectionRejectedError = LoggerMessage.Define<string>(
            Error, new EventId(5, "ConnectionRejectedError"), "Error accepting connection for client '{clientId}'");
        private static Action<ILogger, string, string, int, byte, bool, Exception> logIncomingMessage =
            LoggerMessage.Define<string, string, int, byte, bool>(Trace, new EventId(6, "IncomingMessage"),
                "New message from client '{0}' {{Topic = \"{1}\", Size = {2}, QoS = {3}, Retain = {4}}}");

        internal static void LogAcceptionStarted(this ILogger<MqttServer> logger, Listener listener)
        {
            logAcceptionStarted(logger, listener, null);
        }

        internal static void LogNetworkConnectionAccepted(this ILogger<MqttServer> logger, Listener listener, INetworkConnection connection)
        {
            logConnectionAccepted(logger, listener, connection, null);
        }

        internal static void LogSessionRejectedError(this ILogger<MqttServer> logger, Exception exception, INetworkConnection connection)
        {
            logSessionRejectedError(logger, connection, exception);
        }

        internal static void LogConnectionRejectedError(this ILogger<MqttServer> logger, Exception exception, string clientId)
        {
            logConnectionRejectedError(logger, clientId, exception);
        }

        internal static void LogSessionReplacementError(this ILogger<MqttServer> logger, Exception exception, string clientId)
        {
            logSessionReplacementError(logger, clientId, exception);
        }

        internal static void LogIncomingMessage(this ILogger<MqttServer> logger, string clientId, string topic, int length, byte qos, bool retain)
        {
            logIncomingMessage(logger, clientId, topic, length, qos, retain, null);
        }
    }
}