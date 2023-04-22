using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    private void OnSubscribe(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (header != PacketFlags.SubscribeMask || !SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            MqttPacketHelpers.ThrowInvalidFormat("SUBSCRIBE");
            return;
        }

        if (filters is { Count: 0 })
        {
            ThrowInvalidSubscribePacket();
        }

        var feedback = sessionState!.Subscribe(filters, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(new SubAckPacket(id, feedback));

        subscribeObserver.OnNext(new(sessionState, filters));
    }

    private void OnUnsubscribe(in ReadOnlySequence<byte> reminder)
    {
        if (!UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            MqttPacketHelpers.ThrowInvalidFormat("UNSUBSCRIBE");
        }

        sessionState!.Unsubscribe(filters, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(PacketFlags.UnsubAckPacketMask | id);

        unsubscribeObserver.OnNext(new(sessionState, filters));
    }
}