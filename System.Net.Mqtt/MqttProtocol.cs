using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Threading.Channels;

using static System.Buffers.MemoryPool<byte>;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt;

internal record struct DispatchRecord(MqttPacket Packet, byte[] Buffer, TaskCompletionSource Completion);
public abstract class MqttProtocol : MqttBinaryStreamConsumer
{
    private ChannelReader<DispatchRecord> reader;
    private ChannelWriter<DispatchRecord> writer;
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
        if(!writer.TryWrite(new(packet, null, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected void Post(byte[] buffer)
    {
        if(!writer.TryWrite(new(null, buffer, null)))
        {
            throw new InvalidOperationException(CannotAddOutgoingPacket);
        }
    }

    protected async Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(RunContinuationsAsynchronously);

        if(!writer.TryWrite(new(packet, null, completion)))
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
        var (packet, buffer, completion) = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.AsTask().ConfigureAwait(false);

        try
        {
            if(completion is { Task.IsCompleted: true }) return;

            if(packet is not null)
            {
                var total = packet.GetSize(out var remainingLength);

                using var memory = Shared.Rent(total);
                packet.Write(memory.Memory.Span, remainingLength);
                var svt = Transport.SendAsync(memory.Memory[..total], cancellationToken);
                if(!svt.IsCompletedSuccessfully)
                {
                    await svt.ConfigureAwait(false);
                }
            }
            else if(buffer is not null)
            {
                var svt = Transport.SendAsync(buffer, cancellationToken);
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
        (reader, writer) = Channel.CreateUnbounded<DispatchRecord>(
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