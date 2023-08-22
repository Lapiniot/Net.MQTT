using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        FlushResult result;
        var output = Transport.Output;

        while (await reader!.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var block))
            {
                stoppingToken.ThrowIfCancellationRequested();

                WritePacket(output, ref block);

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

    private void WritePacket(PipeWriter output, ref PacketDescriptor block)
    {
        int size; byte packetType;
        var topic = block.Topic;
        var raw = block.Raw;

        if (!topic.IsEmpty)
        {
            // Decomposed PUBLISH packet
            size = PublishPacket.Write(output, flags: (byte)raw, id: (ushort)(raw >>> 8), topic.Span, block.Payload.Span);
            packetType = (byte)PacketType.PUBLISH;
            goto ret_skip_advance;
        }
        else if ((raw & 0xFF00_0000) is not 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw);
            size = 4;
            packetType = (byte)(raw >>> 28);
        }
        else if (raw is not 0)
        {
            BinaryPrimitives.WriteUInt16BigEndian(output.GetSpan(2), (ushort)raw);
            size = 2;
            packetType = (byte)(raw >>> 12);
        }
        else if (block.Packet is { } packet)
        {
            // Reference to any generic packet implementation
            size = packet.Write(output, out var span);
            packetType = (byte)(span[0] >>> 4);
            goto ret_skip_advance;
        }
        else
        {
            ThrowHelpers.ThrowInvalidDispatchBlock();
            return;
        }

        output.Advance(size);

    ret_skip_advance:
        OnPacketSent(packetType, size);
    }

    protected void Post(IMqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet, null, default, 0)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(null, null, default, value)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, in ReadOnlyMemory<byte> payload)
    {
        if (!writer!.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)))))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private readonly record struct PacketDescriptor(IMqttPacket? Packet, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, uint Raw);
}