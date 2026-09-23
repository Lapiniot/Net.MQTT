using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    private ChannelReader<PacketDescriptor>? reader;
    private ChannelWriter<PacketDescriptor>? writer;
    private readonly int maxUnflushedBytes;
    private TopicAliasMap serverAliases;

    /// <summary>
    /// This value indicates the highest value that the Client will accept as a Topic Alias sent by the Server. 
    /// The Client uses this value to limit the number of Topic Aliases that it is willing to hold on this Connection.
    /// </summary>
    public ushort ClientTopicAliasMaximum { get; init; }

    /// <summary>
    /// Represents the Maximum Packet Size the Client is willing to accept.
    /// </summary>
    public int MaxSendPacketSize { get; init; } = int.MaxValue;

    /// <summary>
    /// Represents topic size which is considered as big enough by the server 
    /// to apply topic/alias mapping for onward delivery
    /// </summary>
    public int TopicAliasSizeThreshold { get; init; } = 128;

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
                    case uint raw32:
                        {
                            BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw32);
                            output.Advance(4);
                            OnPacketSent((PacketType)(raw32 >>> 28), 4);
                            break;
                        }
                    case PublishPacket { QoSLevel: var qos, Id: var id, Topic: var topic } publishPacket:
                        {
                            var newAliasNeedsCommit = false;
                            (ReadOnlyMemory<byte> Topic, ushort Alias) mapping = default;

                            if (ClientTopicAliasMaximum is not 0 &&
                                topic.Length >= TopicAliasSizeThreshold &&
                                serverAliases.TryGetAlias(topic, out mapping, out newAliasNeedsCommit))
                            {
                                (publishPacket.Topic, publishPacket.TopicAlias) = mapping;
                            }

                            var written = publishPacket.Write(output, MaxSendPacketSize);
                            if (written is not 0)
                            {
                                if (newAliasNeedsCommit)
                                {
                                    serverAliases.Commit(ref mapping);
                                }

                                OnPacketSent(PacketType.PUBLISH, written);
                            }
                            else if (qos is not 0)
                            {
                                CompleteMessageDelivery(id);
                            }

                            break;
                        }
                    case IMqttPacket5 packet:
                        {
                            var written = packet.Write(output, MaxSendPacketSize);
                            if (written is not 0)
                            {
                                OnPacketSent((PacketType)descriptor.Tag, written);
                            }

                            break;
                        }
                    case ushort raw16:
                        {
                            BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), raw16);
                            output.Advance(2);
                            OnPacketSent((PacketType)(raw16 >>> 12), 2);
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
    private void Post(PacketDescriptor value)
    {
        if (!writer!.TryWrite(value))
        {
            ThrowHelper.ThrowCannotWriteToQueue();
        }
    }
}