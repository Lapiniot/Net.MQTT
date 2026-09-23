using System.Buffers.Binary;
using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public partial class MqttClient5
{
    private ChannelReader<(PacketDescriptor Descriptor, TaskCompletionSource? Completion)>? reader;
    private ChannelWriter<(PacketDescriptor Descriptor, TaskCompletionSource? Completion)>? writer;
    private TopicAliasMap clientAliases;

    public int MaxSendPacketSize { get; private set; }

    /// <summary>
    /// Gets or sets topic size which is considered as big enough by the client 
    /// to apply topic/alias mapping for onward delivery
    /// </summary>
    public int TopicAliasSizeThreshold { get; set; } = 128;

    protected override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Connection.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                var (packet, tcs) = descriptor;

                if (tcs is { Task.IsCompleted: true })
                {
                    continue;
                }

                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    switch (packet)
                    {
                        case uint raw32:
                            BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw32);
                            output.Advance(4);
                            break;
                        case PublishPacket { Topic: var topic } publishPacket:
                            if (ServerTopicAliasMaximum is not 0 &&
                                topic.Length >= TopicAliasSizeThreshold &&
                                clientAliases.TryGetAlias(topic, out var mapping, out var newNeedsCommit))
                            {
                                (publishPacket.Topic, publishPacket.TopicAlias) = mapping;
                                if (publishPacket.Write(output, MaxSendPacketSize) is not 0)
                                {
                                    if (newNeedsCommit)
                                    {
                                        clientAliases.Commit(ref mapping);
                                    }
                                }
                                else
                                {
                                    tcs?.TrySetException(new PacketTooLargeException());
                                }
                            }
                            else
                            {
                                if (publishPacket.Write(output, MaxSendPacketSize) is 0)
                                {
                                    tcs?.TrySetException(new PacketTooLargeException());
                                }
                            }

                            break;
                        case IMqttPacket5 mqttPacket:
                            if (mqttPacket.Write(output, MaxSendPacketSize) is 0)
                            {
                                tcs?.TrySetException(new PacketTooLargeException());
                            }

                            break;
                        case ushort raw16:
                            BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), raw16);
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
    private void Post(PacketDescriptor packet, TaskCompletionSource? completion = null)
    {
        if (!writer!.TryWrite(new(packet, completion)))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }
}