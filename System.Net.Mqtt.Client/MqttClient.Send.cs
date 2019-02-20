using System.Buffers;
using System.IO;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.QoSLevel;
using static System.String;

namespace System.Net.Mqtt.Client
{
    public delegate void MessageReceivedHandler(MqttClient sender, MqttMessage message);

    public partial class MqttClient : IObservable<MqttMessage>
    {
        public void Publish(string topic, Memory<byte> payload, QoSLevel qosLevel = AtMostOnce, bool retain = false)
        {
            var packet = CreatePublishPacket(topic, payload, qosLevel, retain);

            Post(packet.GetBytes());
        }

        public Task PublishAsync(string topic, Memory<byte> payload, QoSLevel qosLevel = AtMostOnce,
            bool retain = false, CancellationToken cancellationToken = default)
        {
            var packet = CreatePublishPacket(topic, payload, qosLevel, retain);

            return SendAsync(packet.GetBytes(), cancellationToken);
        }

        private PublishPacket CreatePublishPacket(string topic, Memory<byte> payload, QoSLevel qosLevel, bool retain)
        {
            return qosLevel == AtLeastOnce || qosLevel == ExactlyOnce
                ? sessionState.AddPublishToResend(topic, payload, (byte)qosLevel, retain)
                : new PublishPacket(0, 0, topic, payload, retain);
        }

        protected override void OnPubAck(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0100_0000 || !remainder.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBACK"));
            }

            sessionState.RemoveFromResend(id);
        }

        protected override void OnPubRec(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0101_0000 || !remainder.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBREC"));
            }

            var pubRelPacket = sessionState.AddPubRelToResend(id);

            Post(pubRelPacket.GetBytes());
        }

        protected override void OnPubComp(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0111_0000 || !remainder.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBCOMP"));
            }

            sessionState.RemoveFromResend(id);
        }
    }
}