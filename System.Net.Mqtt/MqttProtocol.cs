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

    protected abstract void InitPacketDispatcher();

    protected abstract void CompletePacketDispatch();

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        InitPacketDispatcher();
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);
        dispatchTask = RunPacketDispatcherAsync(Aborted);
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            CompletePacketDispatch();
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
    protected static void WritePublishPacket([NotNull] PipeWriter output, byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, out int written)
    {
        written = PublishPacket.GetSize(flags, topic.Length, payload.Length, out var remainingLength);
        var buffer = output.GetMemory(written);
        PublishPacket.Write(buffer.Span, remainingLength, flags, id, topic.Span, payload.Span);
        output.Advance(written);
    }

    [MethodImpl(AggressiveInlining)]
    protected static void WriteGenericPacket([NotNull] PipeWriter output, [NotNull] MqttPacket packet, out byte packetType, out int written)
    {
        written = packet.GetSize(out var remainingLength);
        var span = output.GetMemory(written).Span;
        packet.Write(span, remainingLength);
        packetType = (byte)(span[0] >> 4);
        output.Advance(written);
    }

    [MethodImpl(AggressiveInlining)]
    protected static void WriteRawPacket([NotNull] PipeWriter output, uint raw)
    {
        var buffer = output.GetMemory(4);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Span, raw);
        output.Advance(4);
    }

    [MethodImpl(AggressiveInlining)]
    protected static void WriteRawPacket([NotNull] PipeWriter output, ushort raw)
    {
        var buffer = output.GetMemory(2);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Span, raw);
        output.Advance(2);
    }
}