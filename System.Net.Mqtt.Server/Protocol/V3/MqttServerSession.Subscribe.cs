using System.Buffers;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    protected sealed override void OnSubscribe(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            ThrowInvalidPacketFormat("SUBSCRIBE");
        }

        if (filters is { Count: 0 })
        {
            ThrowInvalidSubscribePacket();
        }

        var arr = new (string, byte)[filters.Count];
        for (var i = 0; i < filters.Count; i++)
        {
            arr[i] = (UTF8.GetString(filters[i].Item1.Span), filters[i].Item2);
        }

        var feedback = sessionState.Subscribe(arr);

        Post(new SubAckPacket(id, feedback));

        subscribeObserver.OnNext(new(sessionState, arr));
    }

    protected sealed override void OnUnsubscribe(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            ThrowInvalidPacketFormat("UNSUBSCRIBE");
        }

        var arr = new string[filters.Count];
        for (var i = 0; i < filters.Count; i++)
        {
            arr[i] = UTF8.GetString(filters[i].Span);
        }

        sessionState.Unsubscribe(arr);

        Post(PacketFlags.UnsubAckPacketMask | id);
    }
}