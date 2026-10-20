using System.Buffers.Binary;
using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private ChannelReader<(PacketDescriptor Descriptor, TaskCompletionSource? Completion)>? reader;
    private ChannelWriter<(PacketDescriptor Descriptor, TaskCompletionSource? Completion)>? writer;

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

                    var pd = descriptor.Descriptor;

                    switch (pd)
                    {
                        case uint raw32bit:
                            BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw32bit);
                            output.Advance(4);
                            break;
                        case PublishBlock pb:
                            PublishPacket.Write(output,
                                flags: (byte)pb.Flags,
                                id: (ushort)(pb.Flags >>> 8),
                                pb.Topic.Span, pb.Payload.Span);
                            break;
                        case IMqttPacketWriter packet:
                            packet.Write(output);
                            break;
                        case ushort raw16bit:
                            BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), raw16bit);
                            output.Advance(2);
                            break;
                        default:
                            ThrowHelper.ThrowInvalidDispatchBlock();
                            break;
                    }

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

    protected override void CompleteProducer()
    {
        Connection.Output.CancelPendingFlush();
        writer!.TryComplete();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Post(PacketDescriptor descriptor, TaskCompletionSource? completion = null)
    {
        if (!writer!.TryWrite((descriptor, completion)))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }
}