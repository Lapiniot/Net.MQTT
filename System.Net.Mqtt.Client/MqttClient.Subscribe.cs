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
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<QoSLevel[]>> subMap =
            new ConcurrentDictionary<ushort, TaskCompletionSource<QoSLevel[]>>();

        public async Task SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            ushort packetId = idPool.Rent();
            var message = new SubscribeMessage(packetId);
            message.Topics.AddRange(topics);

            TaskCompletionSource<QoSLevel[]> tcs = new TaskCompletionSource<QoSLevel[]>();
            if(subMap.TryAdd(packetId, tcs))
            {
                try
                {
                    await Socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    subMap.TryRemove(packetId, out _);
                    idPool.Return(packetId);
                    throw;
                }

                try
                {
                    await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    subMap.TryRemove(packetId, out _);
                    idPool.Return(packetId);
                }
            }
        }
    }
}