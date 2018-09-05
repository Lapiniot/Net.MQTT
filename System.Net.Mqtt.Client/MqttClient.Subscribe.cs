using System.Collections.Concurrent;
using System.Net.Mqtt.Messages;
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

            var message = new SubscribeMessage(idPool.Rent());
            message.Topics.AddRange(topics);

            return PostMessageWithAcknowledgeAsync(message, subAckCompletions, cancellationToken);
        }

        public Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            var message = new UnsubscribeMessage(idPool.Rent());
            message.Topics.AddRange(topics);

            return PostMessageWithAcknowledgeAsync(message, unsubAckCompletions, cancellationToken);
        }

        private async Task<T> PostMessageWithAcknowledgeAsync<T>(MqttMessageWithId message,
            ConcurrentDictionary<ushort, TaskCompletionSource<T>> storage,
            CancellationToken cancellationToken)
        {
            var packetId = message.PacketId;

            var completionSource = new TaskCompletionSource<T>();
            storage.TryAdd(packetId, completionSource);

            try
            {
                await MqttSendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Cleanup();
                throw;
            }

            try
            {
                return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Cleanup();
            }

            void Cleanup()
            {
                storage.TryRemove(packetId, out _);
                idPool.Return(packetId);
            }
        }

        private void AcknowledgeSubscription(ushort packetId, byte[] result)
        {
            if(subAckCompletions.TryGetValue(packetId, out var tcs)) tcs.TrySetResult(result);
        }

        private void AcknowledgeUnsubscription(ushort packetId)
        {
            if(unsubAckCompletions.TryGetValue(packetId, out var tcs)) tcs.TrySetResult(true);
        }
    }
}