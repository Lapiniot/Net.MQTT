using System.Runtime.CompilerServices;
using static System.Net.Mqtt.PacketType;

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

        this[Publish] = OnPublish;
        this[PubAck] = OnPubAck;
        this[PubRec] = OnPubRec;
        this[PubRel] = OnPubRel;
        this[PubComp] = OnPubComp;
    }

    protected NetworkTransportPipe Transport { get; }

    protected Task DispatchCompletion => dispatchTask;

    protected abstract void OnPublish(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnPubAck(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnPubRec(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnPubRel(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnPubComp(byte header, ReadOnlySequence<byte> reminder);

    protected abstract Task RunPacketDispatcherAsync(CancellationToken stoppingToken);

    protected abstract void InitPacketDispatcher();

    protected abstract void CompletePacketDispatch();

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);
        InitPacketDispatcher();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void WritePublishPacket(PipeWriter output, byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload)
    {
        var total = PublishPacket.GetSize(flags, topic.Length, payload.Length, out var remainingLength);
        var buffer = output.GetMemory(total);
        PublishPacket.Write(buffer.Span, remainingLength, flags, id, topic.Span, payload.Span);
        output.Advance(total);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void WriteGenericPacket(PipeWriter output, MqttPacket packet)
    {
        var total = packet.GetSize(out var remainingLength);
        var buffer = output.GetMemory(total);
        packet.Write(buffer.Span, remainingLength);
        output.Advance(total);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void WriteRawPacket(PipeWriter output, uint raw)
    {
        if ((raw & 0xFF00_0000) > 0)
        {
            var buffer = output.GetMemory(4);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Span, raw);
            output.Advance(4);
        }
        else
        {
            var buffer = output.GetMemory(2);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Span, (ushort)raw);
            output.Advance(2);
        }
    }
}