using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Connection.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();

                switch (descriptor)
                {
                    case uint raw32bit:
                        {
                            BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw32bit);
                            output.Advance(4);
                            OnPacketSent((PacketType)(raw32bit >>> 28), 4);
                            break;
                        }
                    case PublishBlock pb:
                        {
                            var size = PublishPacket.Write(output,
                                flags: (byte)pb.Flags,
                                id: (ushort)(pb.Flags >>> 8),
                                pb.Topic.Span, pb.Payload.Span);
                            OnPacketSent(PacketType.PUBLISH, size);
                            break;
                        }
                    case IMqttPacketWriter packet:
                        {
                            var (size, packetType) = packet.Write(output);
                            OnPacketSent(packetType, size);
                            break;
                        }
                    case ushort raw16bit:
                        {
                            BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), raw16bit);
                            output.Advance(2);
                            OnPacketSent((PacketType)(raw16bit >>> 12), 2);
                            break;
                        }
                    default:
                        ThrowHelper.ThrowInvalidDispatchBlock();
                        break;
                }

                if (output.UnflushedBytes >= maxUnflushedBytes)
                {
                    var bfr = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                    if (bfr.IsCompleted || bfr.IsCanceled)
                    {
                        return;
                    }
                }
            }

            var fr = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
            if (fr.IsCompleted || fr.IsCanceled)
            {
                return;
            }
        }
    }

    protected override void CompleteProducer()
    {
        Connection.Output.CancelPendingFlush();
        writer!.TryComplete();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Post(PacketDescriptor3 descriptor)
    {
        if (!writer!.TryWrite(descriptor))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }
}