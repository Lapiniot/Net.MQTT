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

        var feedback = sessionState!.Subscriptions.Subscribe(filters, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(new SubAckPacket(id, feedback));

        SubscribeObserver.OnNext(new(sessionState.OutgoingWriter, filters));
    }

    private void OnUnsubscribe(in ReadOnlySequence<byte> reminder)
    {
        if (!UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            MqttPacketHelpers.ThrowInvalidFormat("UNSUBSCRIBE");
        }

        sessionState!.Subscriptions.Unsubscribe(filters, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(PacketFlags.UnsubAckPacketMask | id);

        UnsubscribeObserver.OnNext(new(sessionState.OutgoingWriter, filters));
    }
}