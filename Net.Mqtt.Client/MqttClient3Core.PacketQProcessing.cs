namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private ChannelReader<PacketDispatchBlock>? reader;
    private ChannelWriter<PacketDispatchBlock>? writer;

    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Connection.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                var tcs = descriptor.Completion;

                if (tcs is { Task.IsCompleted: true })
                {
                    continue;
                }

                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    descriptor.Descriptor.WriteTo(output);

                    var result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);

                    if (result.IsCanceled)
                    {
                        tcs?.TrySetCanceled(default);
                        return;
                    }

                    tcs?.TrySetResult();

                    if (result.IsCompleted)
                    {
                        return;
                    }
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
    protected void Post(IMqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet)))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(value)))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, TaskCompletionSource? completion = null)
    {
        if (!writer!.TryWrite(new(topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
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