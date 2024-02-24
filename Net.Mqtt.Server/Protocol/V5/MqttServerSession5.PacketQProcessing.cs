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
        FlushResult result;
        var output = Transport.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (packet, raw) = descriptor;

                if ((raw & 0xF000_0000) is not 0)
                {
                    BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw);
                    output.Advance(4);
                    OnPacketSent((PacketType)(raw >>> 28), 4);
                }
                else if (packet is PublishPacket { QoSLevel: var qos, Id: var id, Topic: var topic } publishPacket)
                {
                    var newAliasNeedsCommit = false;
                    if (ClientTopicAliasMaximum is not 0 &&
                        topic.Length >= TopicAliasSizeThreshold &&
                        serverAliases.TryGetAlias(topic, out var mapping, out newAliasNeedsCommit))
                    {
                        (publishPacket.Topic, publishPacket.TopicAlias) = mapping;
                    }

                    var written = publishPacket.Write(output, MaxSendPacketSize);
                    if (written is not 0)
                    {
                        if (newAliasNeedsCommit)
                        {
                            serverAliases.Commit(topic);
                        }

                        OnPacketSent(PacketType.PUBLISH, written);
                    }
                    else if (qos is not 0)
                    {
                        CompleteMessageDelivery(id);
                    }
                }
                else if (packet is not null)
                {
                    var written = packet.Write(output, MaxSendPacketSize);
                    if (written is not 0)
                    {
                        OnPacketSent((PacketType)raw, written);
                    }
                }
                else if (raw is not 0)
                {
                    BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), (ushort)raw);
                    output.Advance(2);
                    OnPacketSent((PacketType)(raw >>> 12), 2);
                }
                else
                {
                    ThrowHelpers.ThrowInvalidDispatchBlock();
                }

                if (output.UnflushedBytes >= maxUnflushedBytes)
                {
                    result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                    if (result.IsCompleted || result.IsCanceled)
                        return;
                }
            }

            result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
            if (result.IsCompleted || result.IsCanceled)
                return;
        }
    }

    private void Post(PubRelPacket packet)
    {
        if (!writer!.TryWrite(new(packet, (uint)PacketType.PUBREL)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private void Post(PubCompPacket packet)
    {
        if (!writer!.TryWrite(new(packet, (uint)PacketType.PUBCOMP)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private void Post(SubAckPacket packet)
    {
        if (!writer!.TryWrite(new(packet, (uint)PacketType.SUBACK)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private void Post(UnsubAckPacket packet)
    {
        if (!writer!.TryWrite(new(packet, (uint)PacketType.UNSUBACK)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private void Post(PublishPacket packet)
    {
        if (!writer!.TryWrite(new(packet, (uint)PacketType.PUBLISH)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private void Post(uint value)
    {
        if (!writer!.TryWrite(new(default, value)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private readonly record struct PacketDescriptor(IMqttPacket5? Packet, uint Raw);
}