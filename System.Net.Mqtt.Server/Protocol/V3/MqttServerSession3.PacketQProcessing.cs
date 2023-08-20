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
        var (packet, topic, payload, raw) = block;

        if (!topic.IsEmpty)
        {
            // Decomposed PUBLISH packet
            var size = PublishPacket.Write(output, flags: (byte)raw, id: (ushort)(raw >>> 8), topic.Span, payload.Span);
            OnPacketSent(0b0011, size);
        }
        else if ((raw & 0xFF00_0000) is not 0)
        {
            WritePacket(output, raw);
            OnPacketSent((byte)(raw >>> 28), 4);
        }
        else if (raw is not 0)
        {
            WritePacket(output, (ushort)raw);
            OnPacketSent((byte)(raw >>> 12), 2);
        }
        else if (packet is not null)
        {
            // Reference to any generic packet implementation
            WritePacket(output, packet, out var packetType, out var written);
            OnPacketSent(packetType, written);
        }
        else
        {
            ThrowHelpers.ThrowInvalidDispatchBlock();
        }
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