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
        if(!SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "SUBSCRIBE"));
        }

        if(filters is { Count: 0 })
        {
            throw new InvalidDataException(InvalidSubscribePacket);
        }

        var feedback = sessionState.Subscribe(filters);

        Post(new SubAckPacket(id, feedback));

        subscribeObserver.OnNext(new SubscriptionRequest(sessionState, filters));
    }

    protected override void OnUnsubscribe(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBSCRIBE"));
        }

        sessionState.Unsubscribe(filters);

        Post(PacketFlags.UnsubAckPacketMask | id);
    }
}