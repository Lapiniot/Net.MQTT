using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading.Channels;

using static System.Buffers.MemoryPool<byte>;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt;

internal record struct DispatchBlock(MqttPacket Packet, string Topic, in ReadOnlyMemory<byte> Buffer, uint Raw, TaskCompletionSource Completion);
public abstract class MqttProtocol : MqttBinaryStreamConsumer
{
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;
    private readonly byte[] rawBuffer = new byte[4];
    private Task queueProcessor;
#pragma warning disable CA2213 // False positive from roslyn analyzer
    private readonly WorkerLoop worker;
#pragma warning restore CA2213
    private readonly bool disposeTransport;

    protected MqttProtocol(NetworkTransport transport, bool disposeTransport) : base(transport?.Reader)
    {
        ArgumentNullException.ThrowIfNull(transport);

        Transport = transport;
        this.disposeTransport = disposeTransport;

        worker = new WorkerLoop(DispatchPacketAsync);
    }

    protected NetworkTransport Transport { get; }

    protected async ValueTask<ReadOnlySequence<byte>> ReadPacketAsync(CancellationToken cancellationToken)
    {
        var reader = Transport.Reader;

        var vt = MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken);

        var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

        var buffer = result.Buffer;

        reader.AdvanceTo(buffer.End);

        return buffer;
    }

    protected void Post(MqttPacket packet)
    {
        if(!writer.TryWrite(new(packet, null, null, 0, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected void PostRaw(byte[] buffer)
    {
        if(!writer.TryWrite(new(null, null, buffer, 0, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected void PostRaw(uint rawPacket)
    {
        if(!writer.TryWrite(new(null, null, null, rawPacket, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected void PostPublish(byte flags, ushort id, string topic, in ReadOnlyMemory<byte> payload)
    {
        if(!writer.TryWrite(new(null, topic, in payload, (uint)(flags | (id << 8)), null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected async Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(RunContinuationsAsynchronously);

        if(!writer.TryWrite(new(packet, null, null, 0, completion)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }

        try
        {
            await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch(OperationCanceledException e) when(e.CancellationToken == cancellationToken)
        {
            completion.TrySetCanceled(cancellationToken);
            throw;
        }
    }

    protected async Task SendPublishAsync(byte flags, ushort id, string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(RunContinuationsAsynchronously);

        if(!writer.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }

        try
        {
            await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch(OperationCanceledException e) when(e.CancellationToken == cancellationToken)
        {
            completion.TrySetCanceled(cancellationToken);
            throw;
        }
    }

    protected async Task DispatchPacketAsync(CancellationToken cancellationToken)
    {
        var rvt = reader.ReadAsync(cancellationToken);
        var (packet, topic, buffer, raw, completion) = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.AsTask().ConfigureAwait(false);

        try
        {
            if(completion is { Task.IsCompleted: true }) return;

            if(packet is not null)
            {
                // Reference to any generic packet implementation
                var total = packet.GetSize(out var remainingLength);

                using var memory = Shared.Rent(total);
                packet.Write(memory.Memory.Span, remainingLength);
                var svt = Transport.SendAsync(memory.Memory[..total], cancellationToken);
                if(!svt.IsCompletedSuccessfully)
                {
                    await svt.ConfigureAwait(false);
                }
            }
            else if(topic is not null)
            {
                // Decomposed PUBLISH packet
                var flags = (byte)(raw & 0xff);
                var id = (ushort)(raw >> 8);

                var total = PublishPacket.GetSize(flags, topic, buffer, out var remainingLength);
                using var memory = Shared.Rent(total);

                PublishPacket.Write(memory.Memory.Span, remainingLength, flags, id, topic, buffer.Span);

                var svt = Transport.SendAsync(memory.Memory[..total], cancellationToken);
                if(!svt.IsCompletedSuccessfully)
                {
                    await svt.ConfigureAwait(false);
                }
            }
            else if(buffer is { Length: > 0 })
            {
                // Precomposed buffer with complete packet data
                var svt = Transport.SendAsync(buffer, cancellationToken);
                if(!svt.IsCompletedSuccessfully)
                {
                    await svt.ConfigureAwait(false);
                }
            }
            else
            {
                // Simple packet with id (4 bytes in size exactly)
                rawBuffer[0] = (byte)(raw >> 24);
                rawBuffer[1] = (byte)(raw >> 16);
                rawBuffer[2] = (byte)(raw >> 8);
                rawBuffer[3] = (byte)raw;
                // or
                // BinaryPrimitives.WriteInt32BigEndian(rawBuffer, raw); 
                // ???
                var svt = Transport.SendAsync(rawBuffer, cancellationToken);
                if(!svt.IsCompletedSuccessfully)
                {
                    await svt.ConfigureAwait(false);
                }
            }

            completion?.TrySetResult();
            OnPacketSent();
        }
        catch(Exception exception)
        {
            completion?.TrySetException(exception);
            throw;
        }
    }

    protected abstract void OnPacketSent();

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        queueProcessor = worker.RunAsync(default);
        return base.StartingAsync(cancellationToken);
    }

    protected override async Task StoppingAsync()
    {
        writer.Complete();

        try
        {
            await queueProcessor.ConfigureAwait(false);
        }
        catch(ChannelClosedException)
        {
            // Expected case
        }

        await worker.StopAsync().ConfigureAwait(false);

        reader = null;
        writer = null;
        queueProcessor = null;

        await base.StoppingAsync().ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        try
        {
            await using(worker.ConfigureAwait(false))
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            if(disposeTransport)
            {
                await Transport.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}