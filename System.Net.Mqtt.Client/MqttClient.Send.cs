using System.Buffers;
using System.IO;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.QoSLevel;
using static System.String;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : IObservable<MqttMessage>
    {
        public void Publish(string topic, Memory<byte> payload, QoSLevel qosLevel = AtMostOnce, bool retain = false)
        {
            var packet = CreatePublishPacket(topic, payload, qosLevel, retain);

            Post(packet);
        }

        public Task PublishAsync(string topic, Memory<byte> payload, QoSLevel qosLevel = AtMostOnce,
            bool retain = false, CancellationToken cancellationToken = default)
        {
            var packet = CreatePublishPacket(topic, payload, qosLevel, retain);

            return SendAsync(packet, cancellationToken);
        }

        private PublishPacket CreatePublishPacket(string topic, Memory<byte> payload, QoSLevel qosLevel, bool retain)
        {
            return qosLevel == AtLeastOnce || qosLevel == ExactlyOnce
                ? sessionState.AddPublishToResend(topic, payload, (byte)qosLevel, retain)
                : new PublishPacket(0, 0, topic, payload, retain);
        }

        protected override void OnPubAck(byte header, ReadOnlySequence<byte> sequence)
        {
            if(header != 0b0100_0000 || !sequence.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBACK"));
            }

            sessionState.RemoveFromResend(id);
        }

        protected override void OnPubRec(byte header, ReadOnlySequence<byte> sequence)
        {
            if(header != 0b0101_0000 || !sequence.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBREC"));
            }

            var pubRelPacket = sessionState.AddPubRelToResend(id);

            Post(pubRelPacket);
        }

        protected override void OnPubComp(byte header, ReadOnlySequence<byte> sequence)
        {
            if(header != 0b0111_0000 || !sequence.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBCOMP"));
            }

            sessionState.RemoveFromResend(id);
        }
    }
}