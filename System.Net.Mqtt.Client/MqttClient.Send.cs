using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public virtual async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        var qos = (byte)qosLevel;
        var flags = (byte)(retain ? PacketFlags.Retain : 0);

        var topicBytes = UTF8.GetBytes(topic);
        var completionSource = new TaskCompletionSource(RunContinuationsAsynchronously);

        if (qos is not (1 or 2))
        {
            PostPublish(flags, 0, topicBytes, payload, completionSource);
        }

        flags |= (byte)(qos << 1);
        var id = await sessionState.CreateMessageDeliveryStateAsync(flags, topicBytes, payload, cancellationToken).ConfigureAwait(false);
        PostPublish(flags, id, topicBytes, payload, completionSource);

        await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    protected internal sealed override void OnPacketSent(byte packetType, int totalLength) { }

    protected sealed override void OnPubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadUInt16(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBACK");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }

    protected sealed override void OnPubRec(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadUInt16(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREC");
        }

        sessionState.SetMessagePublishAcknowledged(id);

        Post(PacketFlags.PubRelPacketMask | id);
    }

    protected sealed override void OnPubComp(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadUInt16(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBCOMP");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }
}