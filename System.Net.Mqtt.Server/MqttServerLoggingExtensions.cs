using System.Net.Connections;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Logging.LogLevel;
using Listener = System.Collections.Generic.IAsyncEnumerable<System.Net.Connections.INetworkConnection>;

namespace System.Net.Mqtt.Server
{
    internal static class MqttServerLoggingExtensions
    {
        private static Action<ILogger, Exception> logGeneralError = LoggerMessage.Define(
            Error, new EventId(0, "GeneralError"),
                "General unexpected error");
        private static Action<ILogger, Listener, Exception> logAcceptionStarted = LoggerMessage.Define<Listener>(
            Information, new EventId(1, "StartAccepting"),
                "{listener}: Ready to accept incoming connections");
        private static Action<ILogger, Listener, INetworkConnection, Exception> logConnectionAccepted = LoggerMessage.Define<Listener, INetworkConnection>(
            Information, new EventId(2, "ConnectionAccepted"),
                "{listener}: New network connection accepted '{connection}'");
        private static Action<ILogger, INetworkConnection, Exception> logSessionError = LoggerMessage.Define<INetworkConnection>(
            Error, new EventId(3, "SessionError"),
                "{connection}: Error running MQTT session on this connection");
        private static Action<ILogger, NetworkTransport, int, Exception> logProtocolVersionMismatch = LoggerMessage.Define<NetworkTransport, int>(
            Warning, new EventId(4, "VersionMismatch"),
                "{connection}: Cannot establish session, client requested unsupported protocol version '{version}'");
        private static Action<ILogger, NetworkTransport, Exception> logInvalidClientId = LoggerMessage.Define<NetworkTransport>(
            Warning, new EventId(5, "InvalidClientId"),
                "{connection}: Cannot establish session, client provided invalid clientId");
        private static Action<ILogger, NetworkTransport, Exception> logMissingConnectPacket = LoggerMessage.Define<NetworkTransport>(
            Warning, new EventId(6, "MissingConnectPacket"),
                "{connection}: Cannot establish session, client didn't send well formed CONNECT packet");
        private static Action<ILogger, string, Exception> logSessionReplacementError = LoggerMessage.Define<string>(
            Warning, new EventId(7, "SessionReplacementError"),
                "{clientId}: Error closing connection for existing session");
        private static Action<ILogger, string, NetworkTransport, Exception> logConnectionRejectedError = LoggerMessage.Define<string, NetworkTransport>(
            Error, new EventId(8, "ConnectionRejectedError"),
                "{clientId}: Error accepting MQTT connection '{connection}'");
        private static Action<ILogger, MqttServerSession, Exception> logSessionStarting =
            LoggerMessage.Define<MqttServerSession>(Information, new EventId(9, "SessionStarting"),
                "{session}: Starting session");
        private static Action<ILogger, MqttServerSession, Exception> logSessionStarted =
            LoggerMessage.Define<MqttServerSession>(Information, new EventId(10, "SessionStarted"),
                "{session}: Session started and ready to process messages");
        private static Action<ILogger, MqttServerSession, Exception> logSessionTerminatedGracefully =
            LoggerMessage.Define<MqttServerSession>(Information, new EventId(11, "SessionTerminatedGracefully"),
                "{session}: Session terminated gracefully");
        private static Action<ILogger, MqttServerSession, Exception> logSessionTerminatedForcibly =
            LoggerMessage.Define<MqttServerSession>(Warning, new EventId(12, "SessionTerminatedForcibly"),
                "{session}: Session terminated forcibly (due to server shutdown)");
        private static Action<ILogger, string, Listener, Exception> logListenerRegistered =
            LoggerMessage.Define<string, Listener>(Warning, new EventId(13, "ListenerRegistered"),
            "Registered new connection listener '{name}' ({listener})");
        private static Action<ILogger, string, string, int, byte, bool, Exception> logIncomingMessage =
            LoggerMessage.Define<string, string, int, byte, bool>(Trace, new EventId(20, "IncomingMessage"),
                "Incoming message from '{clientId}': {{Topic = \"{topic}\", Size = {size}, QoS = {qos}, Retain = {retain}}}");
        private static Action<ILogger, string, string, int, byte, bool, Exception> logOutgoingMessage =
            LoggerMessage.Define<string, string, int, byte, bool>(Trace, new EventId(21, "OutgoingMessage"),
                "Outgoing message for '{clientId}': {{Topic = \"{topic}\", Size = {size}, QoS = {qos}, Retain = {retain}}}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogGeneralError(this ILogger logger, Exception exception)
        {
            logGeneralError(logger, exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogAcceptionStarted(this ILogger logger, Listener listener)
        {
            logAcceptionStarted(logger, listener, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogNetworkConnectionAccepted(this ILogger logger, Listener listener, INetworkConnection connection)
        {
            logConnectionAccepted(logger, listener, connection, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogSessionError(this ILogger logger, Exception exception, INetworkConnection connection)
        {
            logSessionError(logger, connection, exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogConnectionRejectedError(this ILogger logger, Exception exception, string clientId, NetworkTransport transport)
        {
            logConnectionRejectedError(logger, clientId, transport, exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogSessionStarting(this ILogger logger, MqttServerSession session)
        {
            logSessionStarting(logger, session, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogSessionStarted(this ILogger logger, MqttServerSession session)
        {
            logSessionStarted(logger, session, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogSessionTerminatedGracefully(this ILogger logger, MqttServerSession session)
        {
            logSessionTerminatedGracefully(logger, session, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogSessionTerminatedForcibly(this ILogger logger, MqttServerSession session)
        {
            logSessionTerminatedForcibly(logger, session, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogSessionReplacementError(this ILogger logger, Exception exception, string clientId)
        {
            logSessionReplacementError(logger, clientId, exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogIncomingMessage(this ILogger logger, string clientId, string topic, int length, byte qos, bool retain)
        {
            logIncomingMessage(logger, clientId, topic, length, qos, retain, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogOutgoingMessage(this ILogger logger, string clientId, string topic, int length, byte qos, bool retain)
        {
            logOutgoingMessage(logger, clientId, topic, length, qos, retain, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogProtocolVersionMismatch(this ILogger logger, NetworkTransport transport, int version)
        {
            logProtocolVersionMismatch(logger, transport, version, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogMissingConnectPacket(this ILogger logger, NetworkConnectionAdapterTransport transport)
        {
            logMissingConnectPacket(logger, transport, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogInvalidClientId(this ILogger logger, NetworkConnectionAdapterTransport transport)
        {
            logInvalidClientId(logger, transport, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogListenerRegistered(this ILogger logger, string name, Listener listener)
        {
            logListenerRegistered(logger, name, listener, null);
        }
    }
}