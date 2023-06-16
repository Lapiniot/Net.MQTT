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

                try
                {
                    var (packet, raw) = block;

                    try
                    {
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

                        if (output.UnflushedBytes > maxUnflushedBytes)
                        {
                            result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                            if (result.IsCompleted || result.IsCanceled)
                                return;
                        }
                    }
                    catch (ConnectionClosedException)
                    {
                        break;
                    }
                }
                catch (ChannelClosedException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
            if (result.IsCompleted || result.IsCanceled)
                return;
        }
    }

    protected sealed override void OnProducerStartup() => (reader, writer) = Channel.CreateUnbounded<PacketDispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void OnProducerShutdown()
    {
        writer!.TryComplete();
        Transport.Output.CancelPendingFlush();
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