using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        public Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
        {
            var packet = new SubscribePacket(sessionState.Rent(), topics.Select(t => (t.topic, (byte)t.qos)).ToArray());

            return PostPacketAsync<byte[]>(packet, cancellationToken);
        }

        public Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            var packet = new UnsubscribePacket(sessionState.Rent(), topics);

            return PostPacketAsync<object>(packet, cancellationToken);
        }

        protected override void OnSubAck(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b1001_0000 || !SubAckPacket.TryReadPayload(remainder, (int)remainder.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "SUBACK"));
            }

            AcknowledgePacket(packet.Id, packet.Result);
        }

        protected override void OnUnsubAck(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b1011_0000 || !remainder.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBACK"));
            }

            AcknowledgePacket(id);
        }
    }
}