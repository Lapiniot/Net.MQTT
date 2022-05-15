namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    protected sealed override void OnSubscribe(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            MqttPacketHelpers.ThrowInvalidFormat("SUBSCRIBE");
        }

        if (filters is { Count: 0 })
        {
            ThrowInvalidSubscribePacket();
        }

        var feedback = sessionState.Subscribe(filters);

        Post(new SubAckPacket(id, feedback));

        subscribeObserver.OnNext(new(sessionState, filters));
    }

    protected sealed override void OnUnsubscribe(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            MqttPacketHelpers.ThrowInvalidFormat("UNSUBSCRIBE");
        }

        sessionState.Unsubscribe(filters);

        Post(PacketFlags.UnsubAckPacketMask | id);
    }
}