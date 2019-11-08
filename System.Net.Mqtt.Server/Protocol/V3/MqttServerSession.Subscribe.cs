using System.Buffers;
using System.IO;
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

            var result = state.Subscribe(packet.Topics);

            Post(new SubAckPacket(packet.Id, result));

            server.OnSubscribe(state, packet.Topics);
        }

        protected override void OnUnsubscribe(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b10100010 || !UnsubscribePacket.TryReadPayload(buffer, (int)buffer.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBSCRIBE"));
            }

            state.Unsubscribe(packet.Topics);

            Post(new UnsubAckPacket(packet.Id));
        }
    }
}