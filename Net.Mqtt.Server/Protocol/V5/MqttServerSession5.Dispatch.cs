using static Net.Mqtt.PacketType;
using static Net.Mqtt.PacketFlags;
using static Net.Mqtt.Extensions.SequenceExtensions;
using Net.Mqtt.Packets.V5;
using System.Runtime.InteropServices;

namespace Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    private AliasTopicMap clientAliases;
    private uint receivedIncompleteQoS2;

    public required IObserver<IncomingMessage5> IncomingObserver { get; init; }
    public required IObserver<SubscribeMessage5> SubscribeObserver { get; init; }
    public required IObserver<UnsubscribeMessage> UnsubscribeObserver { get; init; }
    public required IObserver<PacketRxMessage> PacketRxObserver { get; init; }
    public required IObserver<PacketTxMessage> PacketTxObserver { get; init; }

    /// <summary>
    /// This value indicates the highest value that the Server will accept as a Topic Alias sent by the Client. 
    /// The Server uses this value to limit the number of Topic Aliases that it is willing to hold on this Connection.
    /// </summary>
    public ushort TopicAliasMaximum { get; init; }

    /// <summary>
    /// The Server uses this value to limit the number of QoS 1 and QoS 2 publications that it is willing to process 
    /// concurrently for the Client. It does not provide a mechanism to limit the QoS 0 publications 
    /// that the Client might try to send.
    /// </summary>
    public ushort ReceiveMaximum { get; init; }

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
            case UNSUBSCRIBE: OnUnsubscribe(header, in reminder); break;
            case PINGREQ: OnPingReq(); break;
            case DISCONNECT: OnDisconnect(in reminder); break;
            case AUTH: OnAuth(header, in reminder); break;
            default: ProtocolErrorException.Throw((byte)type); break;
        }

        OnPacketReceived(type, total);
    }

    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (QoSLevel)((header >>> 1) & QoSMask);
        if (!PublishPacket.TryReadPayloadExact(in reminder, (int)reminder.Length, readPacketId: qos != 0, out var id, out var topic, out var payload, out var props))
        {
            MalformedPacketException.Throw("PUBLISH");
        }

        if (props.TopicAlias is { } alias)
        {
            clientAliases.GetOrUpdateTopic(alias, ref topic);
        }

        var expires = props.MessageExpiryInterval is { } interval ? DateTime.UtcNow.AddSeconds(interval).Ticks : default(long?);

        var message = new Message5(topic, payload, qos, (header & Retain) == Retain)
        {
            ContentType = props.ContentType,
            PayloadFormat = props.PayloadFormat,
            ResponseTopic = props.ResponseTopic,
            CorrelationData = props.CorrelationData,
            UserProperties = props.UserProperties,
            ExpiresAt = expires
        };

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
                    if (receivedIncompleteQoS2 == ReceiveMaximum)
                    {
                        ReceiveMaximumExceededException.Throw(ReceiveMaximum);
                    }

                    receivedIncompleteQoS2++;
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
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBACK");
        }

        CompleteMessageDelivery(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRec(in ReadOnlySequence<byte> reminder)
    {
        if (!PublishResponsePacket.TryReadPayload(in reminder, out var id, out var reasonCode))
        {
            MalformedPacketException.Throw("PUBREC");
        }

        if (reasonCode is < ReasonCode.UnspecifiedError)
        {
            if (state!.SetMessagePublishAcknowledged(id))
                Post(PubRelPacketMask | id);
            else
                Post(new PubRelPacket(id, ReasonCode.PacketIdentifierNotFound));
        }
        else
        {
            CompleteMessageDelivery(id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREL");
        }

        if (state!.RemoveQoS2(id))
        {
            if (receivedIncompleteQoS2 is not 0)
                receivedIncompleteQoS2--;
            Post(PubCompPacketMask | id);
        }
        else
        {
            Post(new PubCompPacket(id, ReasonCode.PacketIdentifierNotFound));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubComp(in ReadOnlySequence<byte> reminder)
    {
        if (!TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBCOMP");
        }

        CompleteMessageDelivery(id);
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

        var result = state!.Subscriptions.Subscribe(filters, subscriptionId);
        ActiveSubscriptions = result.TotalCount;
        Post(new SubAckPacket(id, ImmutableCollectionsMarshal.AsArray(result.Feedback)));
        SubscribeObserver.OnNext(new(state, result.Subscriptions));
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

        if (reasonCode is 0)
        {
            state!.DiscardWillMessageState();
        }

        DisconnectReceived = true;
        Disconnect((DisconnectReason)reasonCode);
    }

    private void OnAuth(byte header, in ReadOnlySequence<byte> reminder) => throw new NotImplementedException();
}