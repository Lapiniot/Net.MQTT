using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    private ChannelReader<PacketDispatchBlock>? reader;
    private ChannelWriter<PacketDispatchBlock>? writer;
    private readonly int maxUnflushedBytes;

    public int MaxSendPacketSize { get; init; } = int.MaxValue;

    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        FlushResult result;
        var output = Transport.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var block))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (packet, raw) = block;

                if (raw > 0)
                {
                    // Simple packet 4 or 2 bytes in size
                    if ((raw & 0xFF00_0000) > 0)
                    {
                        WritePacket(output, raw);
                        OnPacketSent((byte)(raw >> 28), 4);
                    }
                    else
                    {
                        WritePacket(output, (ushort)raw);
                        OnPacketSent((byte)(raw >> 12), 2);
                    }
                }
                else if (packet is not null)
                {
                    // Reference to any generic packet implementation
                    WritePacket(output, packet);
                }
                else
                {
                    ThrowInvalidDispatchBlock();
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

    private void WritePacket(PipeWriter output, IMqttPacket5 packet)
    {
        if (packet is PublishPacket { QoSLevel: var qos, Id: var id, Topic: var topic } publishPacket)
        {
            var newAlias = false;
            if (ClientTopicAliasMaximum is not 0)
            {
                if (serverAliases.TryGetValue(topic, out var existingAlias))
                {
                    publishPacket.TopicAlias = existingAlias;
                    publishPacket.Topic = default;
                }
                else if (nextTopicAlias <= ClientTopicAliasMaximum)
                {
                    newAlias = true;
                    publishPacket.TopicAlias = nextTopicAlias;
                }
            }

            var written = packet.Write(output, MaxSendPacketSize, out _);
            if (written is not 0)
            {
                if (newAlias)
                {
                    serverAliases[topic] = nextTopicAlias++;
                }

                OnPacketSent((byte)PacketType.PUBLISH, written);
            }
            else if (qos is not 0)
            {
                CompleteMessageDelivery(id);
            }
        }
        else
        {
            var written = packet.Write(output, MaxSendPacketSize, out var span);
            if (written is not 0)
            {
                OnPacketSent((byte)(span[0] >> 4), written);
            }
        }
    }

    private void Post(IMqttPacket5 packet)
    {
        if (!writer!.TryWrite(new(packet, default)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    private void Post(uint value)
    {
        if (!writer!.TryWrite(new(default, value)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    private readonly record struct PacketDispatchBlock(IMqttPacket5? Packet, uint Raw);
}