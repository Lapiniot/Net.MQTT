using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient3
    {
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;

        private async Task<T> PostPacketAsync<T>(MqttPacketWithId packet, CancellationToken cancellationToken) where T : class
        {
            var packetId = packet.Id;

            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            pendingCompletions.TryAdd(packetId, completionSource);

            try
            {
                await SendAsync(packet, cancellationToken).ConfigureAwait(false);

                return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as T;
            }
            finally
            {
                pendingCompletions.TryRemove(packetId, out _);
                sessionState.Return(packetId);
            }
        }

        private void AcknowledgePacket(ushort packetId, object result = null)
        {
            if(pendingCompletions.TryGetValue(packetId, out var tcs))
            {
                tcs.TrySetResult(result);
            }
        }
    }
}