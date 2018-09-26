using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        public Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            var packet = new SubscribePacket(idPool.Rent(), topics);

            return PostPacketAsync<byte[]>(packet, cancellationToken);
        }

        public Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            CheckConnected();

            var packet = new UnsubscribePacket(idPool.Rent(), topics);

            return PostPacketAsync<object>(packet, cancellationToken);
        }

        private void OnSubAckPacket(ushort packetId, byte[] result)
        {
            AcknowledgePacket(packetId, result);
        }

        private void OnUnsubAckPacket(ushort packetId)
        {
            AcknowledgePacket(packetId);
        }
    }
}