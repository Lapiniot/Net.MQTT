namespace System.Net.Mqtt;

public abstract class MqttProtocol : MqttBinaryStreamConsumer
{
    private readonly bool disposeTransport;
    private Task dispatchTask;

    protected MqttProtocol(NetworkTransportPipe transport, bool disposeTransport) : base(transport?.Input)
    {
        ArgumentNullException.ThrowIfNull(transport);

        Transport = transport;
        this.disposeTransport = disposeTransport;
    }

    protected NetworkTransportPipe Transport { get; }

    protected Task DispatchCompletion => dispatchTask;

    protected abstract Task RunPacketDispatcherAsync(CancellationToken stoppingToken);

    protected abstract void OnPacketDispatcherStartup();

    protected abstract void OnPacketDispatcherShutdown();

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);
        OnPacketDispatcherStartup();
        dispatchTask = RunPacketDispatcherAsync(Aborted);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            OnPacketDispatcherShutdown();
            await dispatchTask.ConfigureAwait(false);
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