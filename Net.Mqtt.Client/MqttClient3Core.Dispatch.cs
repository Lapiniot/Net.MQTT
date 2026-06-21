using Net.Mqtt.Packets.V3;
using static Net.Mqtt.PacketFlags;
using static Net.Mqtt.PacketType;

namespace Net.Mqtt.Client;

public abstract partial class MqttClient3Core : MqttClient
{
    private readonly int maxInFlight;
    private AsyncSemaphore inflightSentinel;
    private bool connackReceived;

    protected sealed override void Dispatch(byte header, int total, in ReadOnlySequence<byte> reminder)
    {
        var type = (PacketType)(header >>> 4);
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incurring constant number values ordered in the following way
        switch (type)
        {
            case CONNACK: OnConnAck(in reminder); break;
            case PUBLISH: OnPublish(header, in reminder); break;
            case PUBACK: OnPubAck(in reminder); break;
            case PUBREC: OnPubRec(in reminder); break;
            case PUBREL: OnPubRel(in reminder); break;
            case PUBCOMP: OnPubComp(in reminder); break;
            case SUBACK: OnSubAck(in reminder); break;
            case UNSUBACK: OnUnsubAck(in reminder); break;
            case PINGRESP: break;
            default: ProtocolErrorException.Throw((byte)type); break;
        }
    }

    private void OnConnAck(in ReadOnlySequence<byte> reminder)
    {
        if (connackReceived)
        {
            ProtocolErrorException.Throw((byte)CONNACK);
        }

        connackReceived = true;

        try
        {
            if (!ConnAckPacket.TryReadPayload(in reminder, out var packet))
            {
                MalformedPacketException.Throw("CONNACK");
            }

            packet.EnsureSuccessStatusCode();

            CleanSession = !packet.SessionPresent;

            if (CleanSession || sessionState is null)
            {
                sessionState = new();
            }

            if (!CleanSession)
            {
                foreach (var (id, state) in sessionState.PublishState)
                {
                    ResendPublish(id, in state);
                }
            }

            if (connectionOptions.KeepAlive > 0)
            {
                pingWorker = RunPingWorkerAsync(TimeSpan.FromSeconds(connectionOptions.KeepAlive), Aborted);
            }

            OnConnAckSuccess();
        }
        catch (Exception e)
        {
            OnConnAckError(e);
            throw;
        }

        OnConnected(ConnectedEventArgs.GetInstance(CleanSession));
    }

    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (header >>> 1) & QoSMask;
        if (!PublishPacket.TryReadPayloadExact(in reminder, (int)reminder.Length, readPacketId: qos != 0, out var id, out var topic, out var payload))
        {
            MalformedPacketException.Throw("PUBLISH");
        }

        var retain = (header & Retain) == Retain;

        switch (qos)
        {
            case 0:
                DispatchMessage(topic, payload, retain);
                break;

            case 1:
                DispatchMessage(topic, payload, retain);
                Post(PubAckPacketMask | id);
                break;

            case 2:
                if (sessionState!.TryAddQoS2(id))
                {
                    DispatchMessage(topic, payload, retain);
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                MalformedPacketException.Throw("PUBLISH");
                break;
        }
    }

    private void DispatchMessage(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, bool retained)
    {
        var message = new MqttMessage(topic, payload, retained);
        OnMessageReceived(ref message);
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

        sessionState!.SetMessagePublishAcknowledged(id);

        Post(PubRelPacketMask | id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREL");
        }

        sessionState!.RemoveQoS2(id);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CompleteMessageDelivery(ushort id)
    {
        if (sessionState!.DiscardMessageDeliveryState(id))
        {
            OnMessageDeliveryComplete();
            inflightSentinel.TryRelease(1);
        }
    }

    private void OnSubAck(in ReadOnlySequence<byte> reminder)
    {
        if (!SubAckPacket.TryReadPayload(in reminder, (int)reminder.Length, out var packet))
        {
            MalformedPacketException.Throw("SUBACK");
        }

        AcknowledgePacket(packet.Id, packet.Feedback);
    }

    private void OnUnsubAck(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("UNSUBACK");
        }

        AcknowledgePacket(id);
    }

    private void AcknowledgePacket(ushort packetId, object? result = null)
    {
        if (pendingCompletions.TryGetValue(packetId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }
}