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

        if (qos is 0)
        {
            PostPublish(flags, 0, topicBytes, payload, completionSource);
            return;
        }

        flags |= (byte)(qos << 1);
        var id = await sessionState.CreateMessageDeliveryStateAsync(flags, topicBytes, payload, cancellationToken).ConfigureAwait(false);
        PostPublish(flags, id, topicBytes, payload, completionSource);

        await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    protected internal sealed override void OnPacketSent(byte packetType, int totalLength) { }

    protected sealed override void OnPubAck(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBACK");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }

    protected sealed override void OnPubRec(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREC");
        }

        sessionState.SetMessagePublishAcknowledged(id);

        Post(PacketFlags.PubRelPacketMask | id);
    }

    protected sealed override void OnPubComp(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBCOMP");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }
}