using System.Collections.Concurrent;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<byte[]>> subAckCompletions =
            new ConcurrentDictionary<ushort, TaskCompletionSource<byte[]>>();

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<bool>> unsubAckCompletions =
            new ConcurrentDictionary<ushort, TaskCompletionSource<bool>>();

        public Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            var packet = new SubscribePacket(idPool.Rent());
            packet.Topics.AddRange(topics);

            return PostMessageWithAcknowledgeAsync(packet, subAckCompletions, cancellationToken);
        }

        public Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            var packet = new UnsubscribePacket(idPool.Rent());
            packet.Topics.AddRange(topics);

            return PostMessageWithAcknowledgeAsync(packet, unsubAckCompletions, cancellationToken);
        }

        private async Task<T> PostMessageWithAcknowledgeAsync<T>(MqttPacketWithId packet,
            ConcurrentDictionary<ushort, TaskCompletionSource<T>> storage,
            CancellationToken cancellationToken)
        {
            var packetId = packet.Id;

            var completionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            storage.TryAdd(packetId, completionSource);

            try
            {
                await MqttSendPacketAsync(packet, cancellationToken).ConfigureAwait(false);
                return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                storage.TryRemove(packetId, out _);
                idPool.Return(packetId);
            }
        }

        private void OnSubscribeAcknowledgePacket(ushort packetId, byte[] result)
        {
            if(subAckCompletions.TryGetValue(packetId, out var tcs)) tcs.TrySetResult(result);
        }

        private void OnUnsubscribeAcknwledgePacket(ushort packetId)
        {
            if(unsubAckCompletions.TryGetValue(packetId, out var tcs)) tcs.TrySetResult(true);
        }
    }
}