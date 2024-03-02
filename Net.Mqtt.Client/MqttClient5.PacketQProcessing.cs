using System.Buffers.Binary;
using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public partial class MqttClient5
{
    public int MaxSendPacketSize { get; private set; }

    /// <summary>
    /// Gets or sets topic size which is considered as big enough by the client 
    /// to apply topic/alias mapping for onward delivery
    /// </summary>
    public int TopicAliasSizeThreshold { get; set; } = 128;

    private TopicAliasMap clientAliases;

    protected override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                var (packet, raw, tcs) = descriptor;

                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    if ((raw & 0xF000_0000) is not 0)
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw);
                        output.Advance(4);
                    }
                    else if (packet is PublishPacket { Topic: var topic } publishPacket)
                    {
                        if (ServerTopicAliasMaximum is not 0 &&
                            topic.Length >= TopicAliasSizeThreshold &&
                            clientAliases.TryGetAlias(topic, out var mapping, out var newNeedsCommit))
                        {
                            (publishPacket.Topic, publishPacket.TopicAlias) = mapping;
                            if (publishPacket.Write(output, MaxSendPacketSize) is not 0)
                            {
                                if (newNeedsCommit)
                                {
                                    clientAliases.Commit(topic);
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
                    }
                    else if (packet is not null)
                    {
                        if (packet.Write(output, MaxSendPacketSize) is 0)
                        {
                            tcs?.TrySetException(new PacketTooLargeException());
                        }
                    }
                    else if (raw is not 0)
                    {
                        BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), (ushort)raw);
                        output.Advance(2);
                    }
                    else
                    {
                        ThrowHelpers.ThrowInvalidDispatchBlock();
                    }

                    var result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);

                    tcs?.TrySetResult();

                    if (result.IsCompleted || result.IsCanceled)
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

    private void Post(IMqttPacket5 packet, TaskCompletionSource? completion = null)
    {
        if (!writer!.TryWrite(new(packet, default, completion)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private void Post(uint raw)
    {
        if (!writer!.TryWrite(new(null, raw, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private readonly record struct PacketDescriptor(IMqttPacket5? Packet, uint Raw, TaskCompletionSource? Completion);
}