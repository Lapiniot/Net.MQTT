using System.Buffers;
using System.Net.Mqtt.Extensions;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public virtual async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, QoSLevel qosLevel = AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        var qos = (byte)qosLevel;
        var flags = (byte)(retain ? PacketFlags.Retain : 0);

        if (qos is not (1 or 2))
        {
            await SendPublishAsync(flags, 0, topic, payload, cancellationToken).ConfigureAwait(false);
        }

        flags |= (byte)(qos << 1);
        var id = await sessionState.CreateMessageDeliveryStateAsync(flags, topic, payload, cancellationToken).ConfigureAwait(false);
        await SendPublishAsync(flags, id, topic, payload, cancellationToken).ConfigureAwait(false);
    }

    protected sealed override void OnPubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBACK");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }

    protected sealed override void OnPubRec(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBREC");
        }

        sessionState.SetMessagePublishAcknowledged(id);

        Post(PacketFlags.PubRelPacketMask | id);
    }

    protected sealed override void OnPubComp(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBCOMP");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }
}