using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        [Conditional("TRACE")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TraceIncomingMessage(string clientId, string topic, in Memory<byte> payload, in byte qos, in bool retain)
        {
            Logger.LogTrace("New message from client '{0}' (Topic='{1}', Size={2}, QoS={3}, Retain={4})", clientId, topic, payload.Length, qos, retain);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogError(Exception exception, string message = null)
        {
            Logger.LogError(exception, message ?? exception.Message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogInfo(string message)
        {
            Logger.LogInformation(message);
        }
    }
}