using Net.Mqtt.Packets.V5;
using static Net.Mqtt.PacketType;

namespace Net.Mqtt.Client;

public partial class MqttClient5
{
    private readonly int maxInFlight;
    private int receivedIncompleteQoS2;
    private AsyncSemaphore inflightSentinel;

    public ushort ReceiveMaximum { get; private set; }

    protected override void Dispatch(byte header, int total, in ReadOnlySequence<byte> reminder)
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
            case DISCONNECT: break;
            case AUTH: break;
            default: ProtocolErrorException.Throw((byte)type); break;
        }
    }

    private void OnConnAck(in ReadOnlySequence<byte> reminder)
    {
        try
        {
            if (!ConnAckPacket.TryReadPayload(in reminder, out var packet))
            {
                MalformedPacketException.Throw("CONNACK");
            }

            if (packet.AssignedClientId is { IsEmpty: false, Span: var bytes }) ClientId = UTF8.GetString(bytes);
            if (!packet.SessionPresent || sessionState is null) sessionState = new();
            MaxSendPacketSize = (int)packet.MaximumPacketSize.GetValueOrDefault(int.MaxValue);
            var count = int.Min(maxInFlight, packet.ReceiveMaximum);
            inflightSentinel = new(count, count);

            KeepAlive = packet.ServerKeepAlive ?? connectionOptions.KeepAlive;

            OnConnAckSuccess();

            if (KeepAlive is not 0)
            {
                pingCompletion = StartPingWorkerAsync(TimeSpan.FromSeconds(KeepAlive), globalCts.Token);
            }

            OnConnected(ConnectedEventArgs.GetInstance(!packet.SessionPresent));

            if (packet.SessionPresent)
            {
                foreach (var (id, message) in sessionState.PublishState)
                {
                    ResendPublish(id, in message);
                }
            }
        }
        catch (Exception e)
        {
            OnConnAckError(e);
            throw;
        }
    }

    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (header >>> 1) & PacketFlags.QoSMask;
        if (!PublishPacket.TryReadPayloadExact(in reminder, (int)reminder.Length, readPacketId: qos != 0, out var id, out var topic, out var payload, out _))
        {
            MalformedPacketException.Throw("PUBLISH");
        }

        var retained = (header & PacketFlags.Retain) == PacketFlags.Retain;

        switch (qos)
        {
            case 0:
                DispatchMessage(topic, payload, retained);
                break;

            case 1:
                DispatchMessage(topic, payload, retained);
                Post(PacketFlags.PubAckPacketMask | id);
                break;

            case 2:
                if (sessionState.TryAddQoS2(id))
                {
                    if (receivedIncompleteQoS2 == ReceiveMaximum)
                    {
                        ReceiveMaximumExceededException.Throw(ReceiveMaximum);
                    }

                    receivedIncompleteQoS2++;
                    DispatchMessage(topic, payload, retained);
                }

                Post(PacketFlags.PubRecPacketMask | id);
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
        if (!PublishResponsePacket.TryReadPayload(in reminder, out var id, out var reasonCode))
        {
            MalformedPacketException.Throw("PUBREC");
        }

        if (reasonCode is < ReasonCode.UnspecifiedError)
        {
            if (sessionState!.SetMessagePublishAcknowledged(id))
                Post(PacketFlags.PubRelPacketMask | id);
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
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREL");
        }

        if (sessionState!.RemoveQoS2(id))
        {
            if (receivedIncompleteQoS2 is not 0)
                receivedIncompleteQoS2--;
            Post(PacketFlags.PubCompPacketMask | id);
        }
        else
        {
            Post(new PubCompPacket(id, ReasonCode.PacketIdentifierNotFound));
        }
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
        if (!UnsubAckPacket.TryReadPayload(in reminder, (int)reminder.Length, out var packet))
        {
            MalformedPacketException.Throw("UNSUBACK");
        }

        AcknowledgePacket(packet.Id);
    }
}