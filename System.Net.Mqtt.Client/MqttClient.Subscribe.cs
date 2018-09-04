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

        public async Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            ushort packetId = idPool.Rent();
            var message = new SubscribeMessage(packetId);
            message.Topics.AddRange(topics);

            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
            subAckCompletions.TryAdd(packetId, tcs);

            try
            {
                await Socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                CleanupSubscriptionState(packetId);
                throw;
            }

            try
            {
                return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CleanupSubscriptionState(packetId);
            }
        }

        private void CleanupSubscriptionState(ushort packetId)
        {
            subAckCompletions.TryRemove(packetId, out _);
            idPool.Return(packetId);
        }

        private void AcknowlegeSubscription(ushort packetId, byte[] result)
        {
            if(subAckCompletions.TryGetValue(packetId, out var tcs)) tcs.TrySetResult(result);
        }
    }
}