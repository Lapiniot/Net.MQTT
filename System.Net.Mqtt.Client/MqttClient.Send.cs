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
        await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
        var id = sessionState.CreateMessageDeliveryState(flags, topicBytes, payload);
        PostPublish(flags, id, topicBytes, payload, completionSource);

        await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnPubAck(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBACK");
        }

        if (sessionState.DiscardMessageDeliveryState(id))
            inflightSentinel.TryRelease(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnPubRec(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREC");
        }

        sessionState.SetMessagePublishAcknowledged(id);

        Post(PacketFlags.PubRelPacketMask | id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnPubComp(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBCOMP");
        }

        if (sessionState.DiscardMessageDeliveryState(id))
            inflightSentinel.TryRelease(1);
    }
}