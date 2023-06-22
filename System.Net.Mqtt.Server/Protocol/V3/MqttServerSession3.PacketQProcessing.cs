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

                var (packet, topic, payload, raw) = block;

                if (!topic.IsEmpty)
                {
                    // Decomposed PUBLISH packet
                    var flags = (byte)(raw & 0xff);
                    var size = PublishPacket.GetSize(flags, topic.Length, payload.Length, out var remainingLength);
                    var buffer = output.GetMemory(size);
                    PublishPacket.Write(buffer.Span, remainingLength, flags, (ushort)(raw >> 8), topic.Span, payload.Span);
                    output.Advance(size);
                    OnPacketSent(0b0011, size);
                }
                else if (raw > 0)
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
                    WritePacket(output, packet, out var packetType, out var written);
                    OnPacketSent(packetType, written);
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

    protected void Post(MqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet, null, default, 0)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(null, null, default, value)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, in ReadOnlyMemory<byte> payload)
    {
        if (!writer!.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)))))
        {
            ThrowCannotWriteToQueue();
        }
    }

    private readonly record struct DispatchBlock(MqttPacket? Packet, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Buffer, uint Raw);
}