using System.Net.Mqtt.Packets.V5;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    private readonly Dictionary<ushort, ReadOnlyMemory<byte>> clientAliases;
    public required IObserver<IncomingMessage5> IncomingObserver { get; init; }
    public required IObserver<SubscribeMessage5> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }

    /// <summary>
    /// This value indicates the highest value that the Server will accept as a Topic Alias sent by the Client. 
    /// The Server uses this value to limit the number of Topic Aliases that it is willing to hold on this Connection.
    /// </summary>
    public ushort ServerTopicAliasMaximum { get; init; }

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
            case DISCONNECT: OnDisconnect(in reminder); break;
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
                clientAliases[alias] = topic;
            }
            else if (!clientAliases.TryGetValue(alias, out currentTopic))
            {
                ProtocolErrorException.Throw();
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
                IncomingObserver.OnNext(new(state!, message));
                break;

            case 1:
                IncomingObserver.OnNext(new(state!, message));
                Post(PubAckPacketMask | id);
                break;

            case 2:
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

        SubscribeObserver.OnNext(new(state!, filters));
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

    private void OnDisconnect(in ReadOnlySequence<byte> reminder)
    {
        if (!DisconnectPacket.TryReadPayload(reminder, out var reasonCode, out _, out _, out _, out _))
        {
            MalformedPacketException.Throw("DISCONNECT");
        }

        if (reasonCode == DisconnectPacket.Normal)
        {
            state!.DiscardWillMessageState();
        }

        DisconnectReceived = true;
        Disconnect((DisconnectReason)reasonCode);
    }

    private void OnAuth(byte header, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
}