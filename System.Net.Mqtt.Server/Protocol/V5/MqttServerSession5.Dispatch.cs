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
            default: ProtocolErrorException.Throw((byte)type); break;
        }
    }

    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (header >> 1) & QoSMask;
        if (!PublishPacket.TryReadPayload(in reminder, qos != 0, (int)reminder.Length, out var id, out var topic, out var payload, out var props))
        {
            MalformedPacketException.Throw("PUBLISH");
        }

        ReadOnlyMemory<byte> currentTopic = topic;

        if (props.TopicAlias is { } alias)
        {
            if (alias is 0 || alias > ServerTopicAliasMaximum)
            {
                InvalidTopicAliasException.Throw();
            }

            if (topic.Length is not 0)
            {
                aliases[alias] = topic;
            }
            else if (!aliases.TryGetValue(alias, out currentTopic))
            {
                InvalidTopicAliasException.Throw();
            }
        }

        var expires = props.MessageExpiryInterval is { } interval ? DateTime.UtcNow.AddSeconds(interval).Ticks : default(long?);

        var message = new Message5(currentTopic, payload, (byte)qos, (header & Retain) == Retain)
        {
            ContentType = props.ContentType,
            PayloadFormat = props.PayloadFormat.GetValueOrDefault(),
            ResponseTopic = props.ResponseTopic,
            CorrelationData = props.CorrelationData,
            Properties = props.UserProperties,
            ExpiresAt = expires
        };

        switch (qos)
        {
            case 0:
                IncomingObserver.OnNext(new(message, state!));
                break;

            case 1:
                IncomingObserver.OnNext(new(message, state!));
                Post(PubAckPacketMask | id);
                break;

            case 2:
                // This is to avoid message duplicates for QoS 2
                if (state!.TryAddQoS2(id))
                {
                    IncomingObserver.OnNext(new(message, state!));
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                MalformedPacketException.Throw("PUBLISH");
                break;
        }
    }

    private void OnPubAck(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBACK");
        }

        state!.DiscardMessageDeliveryState(id);
    }

    private void OnPubRec(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREC");
        }

        state!.SetMessagePublishAcknowledged(id);
        Post(PubRelPacketMask | id);
    }

    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREL");
        }

        state!.RemoveQoS2(id);
        Post(PubCompPacketMask | id);
    }

    private void OnPubComp(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBCOMP");
        }

        state!.DiscardMessageDeliveryState(id);
    }

    private void OnSubscribe(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (header != SubscribeMask ||
            !SubscribePacket.TryReadPayload(in reminder, (int)reminder.Length, out var id, out var subscriptionId, out _, out var filters) ||
            subscriptionId == 0 ||
            filters is { Count: 0 })
        {
            MalformedPacketException.Throw("SUBSCRIBE");
            return;
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
            MalformedPacketException.Throw("UNSUBSCRIBE");
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
        Disconnect(DisconnectReason.NormalClosure);
    }

    private void OnAuth(byte header, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
}