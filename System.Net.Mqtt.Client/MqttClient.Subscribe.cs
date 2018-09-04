using System.Collections.Concurrent;
using System.Net.Mqtt.Messages;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Sockets.SocketFlags;

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

            return PostMessageWithAknowledgeAsync(message, subAckCompletions, cancellationToken);
        }

        public Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            var message = new UnsubscribeMessage(idPool.Rent());
            message.Topics.AddRange(topics);

            return PostMessageWithAknowledgeAsync(message, unsubAckCompletions, cancellationToken);
        }

        private async Task<T> PostMessageWithAknowledgeAsync<T>(MqttMessageWithId message,
            ConcurrentDictionary<ushort, TaskCompletionSource<T>> storage,
            CancellationToken cancellationToken)
        {
            ushort packetId = message.PacketId;

            TaskCompletionSource<T> completionSource = new TaskCompletionSource<T>();
            storage.TryAdd(packetId, completionSource);

            try
            {
                await Socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);
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

        private void AcknowlegeSubscription(ushort packetId, byte[] result)
        {
            if(subAckCompletions.TryGetValue(packetId, out var tcs)) tcs.TrySetResult(result);
        }

        private void AcknowlegeUnsubscription(ushort packetId)
        {
            if(unsubAckCompletions.TryGetValue(packetId, out var tcs)) tcs.TrySetResult(true);
        }
    }
}