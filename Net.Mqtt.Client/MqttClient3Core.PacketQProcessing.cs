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
                        case ReadOnlyMemory<byte> topic:
                            PublishPacket.Write(output, flags: pd.Flags, id: pd.Id, topic.Span, pd.Payload.Span);
                            break;
                        case IMqttPacket packet:
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
    private void Post(PacketDescriptor value)
    {
        if (!writer!.TryWrite((value, null)))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Post(ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, byte flags, TaskCompletionSource? completion = null)
    {
        if (!writer!.TryWrite((PacketDescriptor.IUnionMembers.Create(id, topic, payload, flags), completion)))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }
}