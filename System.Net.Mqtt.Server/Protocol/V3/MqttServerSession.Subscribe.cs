using System.Buffers;
using System.Net.Mqtt.Packets;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    protected override void OnSubscribe(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var topics))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "SUBSCRIBE"));
        }

        var result = sessionState.Subscribe(topics);

        Post(new SubAckPacket(id, result));

        subscribeObserver.OnNext(new SubscriptionRequest(sessionState, topics));
    }

    protected override void OnUnsubscribe(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var topics))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBSCRIBE"));
        }

        sessionState.Unsubscribe(topics);

        PostRaw(PacketFlags.UnsubAckPacketMask | id);
    }
}