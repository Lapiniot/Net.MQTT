using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Mqtt.Packets;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class MqttServerSession
    {
        protected override void OnSubscribe(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b10000010 || !SubscribePacket.TryReadPayload(buffer, (int)buffer.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "SUBSCRIBE"));
            }

            var array = packet.Topics.ToArray();

            var result = state.Subscribe(array);

            Post(new SubAckPacket(packet.Id, result));

            subscribeObserver?.OnNext(new SubscriptionRequest(state, array));
        }

        protected override void OnUnsubscribe(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b10100010 || !UnsubscribePacket.TryReadPayload(buffer, (int)buffer.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBSCRIBE"));
            }

            state.Unsubscribe(packet.Topics.ToArray());

            Post(new UnsubAckPacket(packet.Id));
        }
    }
}