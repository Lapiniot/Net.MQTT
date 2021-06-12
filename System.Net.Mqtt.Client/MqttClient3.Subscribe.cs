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
    public partial class MqttClient3
    {
        protected bool ConnectionAcknowledged { get; private set; }

        public async Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
        {
            if(!ConnectionAcknowledged)
            {
                await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
            }

            return await SendPacketAsync<byte[]>(
                id => new SubscribePacket(id, topics.Select(t => (t.topic, (byte)t.qos)).ToArray()),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            if(!ConnectionAcknowledged)
            {
                await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
            }

            await SendPacketAsync<object>(id => new UnsubscribePacket(id, topics), cancellationToken).ConfigureAwait(false);
        }

        protected override void OnSubAck(byte header, ReadOnlySequence<byte> reminder)
        {
            if(!SubAckPacket.TryReadPayload(reminder, (int)reminder.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "SUBACK"));
            }

            AcknowledgePacket(packet.Id, packet.Result);
        }

        protected override void OnUnsubAck(byte header, ReadOnlySequence<byte> reminder)
        {
            if(!reminder.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBACK"));
            }

            AcknowledgePacket(id);
        }
    }
}