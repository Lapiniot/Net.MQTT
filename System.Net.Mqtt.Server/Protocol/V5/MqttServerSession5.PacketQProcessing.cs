namespace System.Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
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

    private void Post(MqttPacket packet)
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

    private readonly record struct PacketDispatchBlock(MqttPacket? Packet, uint Raw);
}