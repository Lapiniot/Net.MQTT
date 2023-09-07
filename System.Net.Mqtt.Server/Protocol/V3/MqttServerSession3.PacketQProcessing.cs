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
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var size = descriptor.WriteTo(output, out var packetType);
                OnPacketSent(packetType, size);

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

    protected void Post(SubAckPacket packet)
    {
        if (!writer!.TryWrite(new(packet, (byte)PacketType.SUBACK)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(value)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, in ReadOnlyMemory<byte> payload)
    {
        if (!writer!.TryWrite(new(topic, payload, (uint)(flags | (id << 8)))))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }
}