using System.Net.Mqtt.Packets.V3;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private ChannelReader<PacketDispatchBlock> reader;
    private ChannelWriter<PacketDispatchBlock> writer;

    public override async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload,
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

    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();
                var tcs = descriptor.Completion;
                if (tcs is { Task.IsCompleted: true })
                    return;

                try
                {

                    descriptor.Descriptor.WriteTo(output, out _);

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

        if (sessionState.DiscardMessageDeliveryState(id))
            inflightSentinel.TryRelease(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRec(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREC");
        }

        sessionState.SetMessagePublishAcknowledged(id);

        Post(PacketFlags.PubRelPacketMask | id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubComp(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBCOMP");
        }

        if (sessionState.DiscardMessageDeliveryState(id))
            inflightSentinel.TryRelease(1);
    }

    protected void Post(ConnectPacket packet)
    {
        if (!writer.TryWrite(new(packet, (byte)PacketType.CONNECT, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(SubscribePacket packet)
    {
        if (!writer.TryWrite(new(packet, (byte)PacketType.SUBSCRIBE, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(UnsubscribePacket packet)
    {
        if (!writer.TryWrite(new(packet, (byte)PacketType.UNSUBSCRIBE, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer.TryWrite(new(value)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private readonly struct PacketDispatchBlock
    {
        public PacketDispatchBlock(uint value) => Descriptor = new(value);

        public PacketDispatchBlock(IMqttPacket packet, byte packetType, TaskCompletionSource completion)
        {
            Descriptor = new(packet, packetType);
            Completion = completion;
        }

        public PacketDispatchBlock(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, uint flags, TaskCompletionSource completion)
        {
            Descriptor = new(topic, payload, flags);
            Completion = completion;
        }

        public PacketDescriptor Descriptor { get; }
        public TaskCompletionSource Completion { get; }
    }
}