using static System.Threading.Tasks.TaskCreationOptions;

namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private ChannelReader<PacketDispatchBlock>? reader;
    private ChannelWriter<PacketDispatchBlock>? writer;

    public override async Task PublishAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        var qos = (byte)qosLevel;
        var flags = (byte)(retain ? PacketFlags.Retain : 0);

        var completionSource = new TaskCompletionSource(RunContinuationsAsynchronously);
        ushort id = 0;

        try
        {
            if (qos is 0)
            {
                PostPublish(flags, 0, topic, payload, completionSource);
            }
            else
            {
                flags |= (byte)(qos << 1);
                await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
                id = sessionState!.CreateMessageDeliveryState(new PublishDeliveryState(flags, topic, payload));
                OnMessageDeliveryStarted();

                PostPublish(flags, id, topic, payload, completionSource);
            }

            await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (id is not 0)
            {
                CompleteMessageDelivery(id);
            }

            throw;
        }
    }

    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();
                var tcs = descriptor.Completion;
                if (tcs is { Task.IsCompleted: true })
                    return;

                try
                {

                    descriptor.Descriptor.WriteTo(output);

                    var result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);

                    tcs?.TrySetResult();

                    if (result.IsCompleted || result.IsCanceled)
                        return;
                }
                catch (ChannelClosedException)
                {
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    tcs?.TrySetException(ex);
                    throw;
                }
            }
        }
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

        Post(PacketFlags.PubRelPacketMask | id);
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
    protected void Post(IMqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(value)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, TaskCompletionSource? completion = null)
    {
        if (!writer!.TryWrite(new(topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private readonly struct PacketDispatchBlock
    {
        public PacketDispatchBlock(uint value) => Descriptor = new(value);

        public PacketDispatchBlock(IMqttPacket packet) => Descriptor = new(packet);

        public PacketDispatchBlock(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, uint flags, TaskCompletionSource? completion)
        {
            Descriptor = new(topic, payload, flags);
            Completion = completion;
        }

        public PacketDescriptor Descriptor { get; }
        public TaskCompletionSource? Completion { get; }
    }
}