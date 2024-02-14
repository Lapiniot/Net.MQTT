using System.Net.Mqtt.Packets.V3;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.PacketFlags;
using SequenceExtensions = System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    protected sealed override void Dispatch(byte header, int total, in ReadOnlySequence<byte> reminder)
    {
        var type = (PacketType)(header >>> 4);
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incurring constant number values ordered in the following way
        switch (type)
        {
            case CONNECT: break;
            case PUBLISH: OnPublish(header, in reminder); break;
            case PUBACK: OnPubAck(in reminder); break;
            case PUBREC: OnPubRec(in reminder); break;
            case PUBREL: OnPubRel(in reminder); break;
            case PUBCOMP: OnPubComp(in reminder); break;
            case SUBSCRIBE: OnSubscribe(header, in reminder); break;
            case UNSUBSCRIBE: OnUnsubscribe(in reminder); break;
            case PINGREQ: OnPingReq(); break;
            case DISCONNECT: OnDisconnect(); break;
            default: ProtocolErrorException.Throw((byte)type); break;
        }

        OnPacketReceived(type, total);
    }

    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (QoSLevel)((header >>> 1) & QoSMask);
        if (!PublishPacket.TryReadPayloadExact(in reminder, (int)reminder.Length, readPacketId: qos != 0, out var id, out var topic, out var payload))
        {
            MalformedPacketException.Throw("PUBLISH");
        }

        var message = new Message3(topic, payload, qos, (header & Retain) == Retain);

        switch (qos)
        {
            case QoSLevel.QoS0:
                IncomingObserver.OnNext(new(state!, message));
                break;

            case QoSLevel.QoS1:
                IncomingObserver.OnNext(new(state!, message));
                Post(PubAckPacketMask | id);
                break;

            case QoSLevel.QoS2:
                // This is to avoid message duplicates for QoS 2
                if (state!.TryAddQoS2(id))
                {
                    IncomingObserver.OnNext(new(state!, message));
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                MalformedPacketException.Throw("PUBLISH");
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubAck(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBACK");
        }

        CompleteMessageDelivery(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRec(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREC");
        }

        state!.SetMessagePublishAcknowledged(id);
        Post(PubRelPacketMask | id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREL");
        }

        state!.RemoveQoS2(id);
        Post(PubCompPacketMask | id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubComp(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBCOMP");
        }

        CompleteMessageDelivery(id);
    }

    private void OnSubscribe(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (header != SubscribeMask ||
            !SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters) ||
            filters is { Count: 0 })
        {
            MalformedPacketException.Throw("SUBSCRIBE");
            return;
        }

        var feedback = state!.Subscriptions.Subscribe(filters, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(new SubAckPacket(id, feedback));

        SubscribeObserver.OnNext(new(state, filters));
    }

    private void OnUnsubscribe(in ReadOnlySequence<byte> reminder)
    {
        if (!UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var filters))
        {
            MalformedPacketException.Throw("UNSUBSCRIBE");
        }

        state!.Subscriptions.Unsubscribe(filters, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(UnsubAckPacketMask | id);

        UnsubscribeObserver.OnNext(new(filters));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPingReq() => Post(PingRespPacket);

    private void OnDisconnect()
    {
        // Graceful disconnection: no need to dispatch last will message
        state!.WillMessage = null;
        DisconnectReceived = true;
        Disconnect(DisconnectReason.Normal);
    }
}