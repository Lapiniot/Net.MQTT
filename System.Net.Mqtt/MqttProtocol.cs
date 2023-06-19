﻿namespace System.Net.Mqtt;

public abstract class MqttProtocol : MqttBinaryStreamConsumer
{
    private readonly bool disposeTransport;

    protected MqttProtocol(NetworkTransportPipe transport, bool disposeTransport) : base(transport?.Input)
    {
        ArgumentNullException.ThrowIfNull(transport);

        Transport = transport;
        this.disposeTransport = disposeTransport;
    }

    protected NetworkTransportPipe Transport { get; }

    public Task Completion { get; private set; }
    public Task ProducerCompletion { get; private set; }

    protected abstract Task RunProducerAsync(CancellationToken stoppingToken);

    protected abstract void OnProducerStartup();

    protected abstract void OnProducerShutdown();

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        // Initialize and start outgoing data producer task first to avoid race condition when
        // incoming data consumer task starts generating outgoing packets immidiatelly 
        // in response to very first incoming packets, but producer is not yet ready.
        OnProducerStartup();
        ProducerCompletion = RunProducerAsync(Aborted);
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);
        Completion = WaitCompletedAsync();
    }

    protected virtual Task WaitCompletedAsync() => Task.WhenAny(ProducerCompletion, ConsumerCompletion);

    protected override async Task StoppingAsync()
    {
        try
        {
            OnProducerShutdown();
            await ProducerCompletion.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* expected */ }
        finally
        {
            await base.StoppingAsync().ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (disposeTransport)
            {
                await Transport.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    [MethodImpl(AggressiveInlining)]
    protected static void WritePacket([NotNull] PipeWriter output, [NotNull] MqttPacket packet, out byte packetType, out int written)
    {
        written = packet.Write(output, out var span);
        packetType = (byte)(span[0] >> 4);
    }

    [MethodImpl(AggressiveInlining)]
    protected static void WritePacket([NotNull] PipeWriter output, uint raw)
    {
        var buffer = output.GetMemory(4);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Span, raw);
        output.Advance(4);
    }

    [MethodImpl(AggressiveInlining)]
    protected static void WritePacket([NotNull] PipeWriter output, ushort raw)
    {
        var buffer = output.GetMemory(2);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Span, raw);
        output.Advance(2);
    }
}