using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private readonly IPacketIdPool idPool;

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;

        private async Task MqttDisconnectAsync()
        {
            await SendAsync(new byte[] {(byte)PacketType.Disconnect, 0}, default).ConfigureAwait(false);
        }

        private Task MqttSendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            return MqttSendPacketAsync(packet.GetBytes(), cancellationToken);
        }

        private async Task MqttSendPacketAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
        {
            await SendAsync(bytes, cancellationToken).ConfigureAwait(false);

            pingWorker.ResetDelay();
        }

        private async Task<T> PostPacketAsync<T>(MqttPacketWithId packet, CancellationToken cancellationToken) where T : class
        {
            var packetId = packet.Id;

            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            pendingCompletions.TryAdd(packetId, completionSource);

            try
            {
                await MqttSendPacketAsync(packet, cancellationToken).ConfigureAwait(false);

                return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as T;
            }
            finally
            {
                pendingCompletions.TryRemove(packetId, out _);
                idPool.Return(packetId);
            }
        }

        private void AcknowledgePacket(ushort packetId, object result = null)
        {
            if(pendingCompletions.TryGetValue(packetId, out var tcs))
            {
                tcs.TrySetResult(result);
            }
        }

        private async Task ResendPublishPacketsAsync()
        {
            foreach(var p in publishFlowPackets)
            {
                await MqttSendPacketAsync(p).ConfigureAwait(true);
            }
        }
    }
}