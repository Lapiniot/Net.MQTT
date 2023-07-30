namespace System.Net.Mqtt;

public abstract class MqttSession : MqttBinaryStreamConsumer
{
    private readonly bool disposeTransport;

    protected MqttSession(NetworkTransportPipe transport, bool disposeTransport) : base(transport?.Input)
    {
        ArgumentNullException.ThrowIfNull(transport);

        Transport = transport;
        this.disposeTransport = disposeTransport;
    }

    protected NetworkTransportPipe Transport { get; }

    public Task ProducerCompletion { get; private set; }

    protected abstract Task RunProducerAsync(CancellationToken stoppingToken);

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        ProducerCompletion = RunProducerAsync(Aborted);
        return base.StartingAsync(cancellationToken);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            Abort();
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
    protected static void WritePacket([NotNull] PipeWriter output, [NotNull] IMqttPacket packet, out byte packetType, out int written)
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