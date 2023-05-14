using System.Net.Mqtt.Packets.V5;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    protected sealed override void Dispatch(PacketType type, byte header, in ReadOnlySequence<byte> reminder)
    {
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
            case UNSUBSCRIBE: OnUnsubscribe(header, in reminder); break;
            case PINGREQ: OnPingReq(); break;
            case DISCONNECT: OnDisconnect(); break;
            case AUTH: OnAuth(header, in reminder); break;
            default: MqttPacketHelpers.ThrowUnexpectedType((byte)type); break;
        }
    }

    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (header >> 1) & QoSMask;
        if (!PublishPacket.TryReadPayload(in reminder, qos != 0, (int)reminder.Length, out var id, out var topic, out var payload, out _))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBLISH");
        }

        var message = new Message5(topic, payload, (byte)qos, (header & Retain) == Retain);

        switch (qos)
        {
            case 0:
                IncomingObserver.OnNext(new(message, ClientId));
                break;

            case 1:
                IncomingObserver.OnNext(new(message, ClientId));
                Post(PubAckPacketMask | id);
                break;

            case 2:
                // This is to avoid message duplicates for QoS 2
                if (state!.TryAddQoS2(id))
                {
                    IncomingObserver.OnNext(new(message, ClientId));
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                MqttPacketHelpers.ThrowInvalidFormat("PUBLISH");
                break;
        }
    }

    private void OnPubAck(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBACK");
        }

        state!.DiscardMessageDeliveryState(id);
    }

    private void OnPubRec(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREC");
        }

        state!.SetMessagePublishAcknowledged(id);
        Post(PubRelPacketMask | id);
    }

    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREL");
        }

        state!.RemoveQoS2(id);
        Post(PubCompPacketMask | id);
    }

    private void OnPubComp(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBCOMP");
        }

        state!.DiscardMessageDeliveryState(id);
    }

    private void OnSubscribe(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (header != SubscribeMask
            || !SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var subscriptionId, out _, out var filters)
            || subscriptionId == 0)
        {
            MqttPacketHelpers.ThrowInvalidFormat("SUBSCRIBE");
            return;
        }

        if (filters is { Count: 0 })
        {
            ThrowInvalidSubscribePacket();
        }

        var feedback = state!.Subscriptions.Subscribe(filters, subscriptionId, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(new SubAckPacket(id, feedback));

        SubscribeObserver.OnNext(new(state.OutgoingWriter, filters));
    }

    private void OnUnsubscribe(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (header != UnsubscribeMask || !UnsubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out _, out var filters))
        {
            MqttPacketHelpers.ThrowInvalidFormat("UNSUBSCRIBE");
            return;
        }

        var feedback = state!.Subscriptions.Unsubscribe(filters, out var currentCount);
        ActiveSubscriptions = currentCount;

        Post(new UnsubAckPacket(id, feedback));

        UnsubscribeObserver.OnNext(new(filters));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPingReq() => Post(PingRespPacket);

    private void OnDisconnect()
    {
        DisconnectReceived = true;
        StopAsync().Observe();
    }

    private void OnAuth(byte header, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
}