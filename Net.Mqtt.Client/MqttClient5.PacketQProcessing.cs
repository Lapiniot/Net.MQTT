using System.Buffers.Binary;

namespace Net.Mqtt.Client;

public partial class MqttClient5
{
    public int MaxSendPacketSize { get; private set; }

    protected override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (packet, raw, tcs) = descriptor;

                if (tcs is { Task.IsCompleted: true })
                {
                    return;
                }

                try
                {
                    if ((raw & 0xF000_0000) is not 0)
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw);
                        output.Advance(4);
                    }
                    else if (packet is not null)
                    {
                        if (packet.Write(output, MaxSendPacketSize) is 0)
                        {
                            tcs?.SetException(new PacketTooLargeException());
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
                catch (OperationCanceledException)
                {
                    tcs?.TrySetCanceled(stoppingToken);
                    return;
                }
                catch (Exception ex)
                {
                    tcs?.TrySetException(ex);
                    throw;
                }
            }
        }
    }

    private void Post(IMqttPacket5 packet, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(packet, default, completion)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private void Post(uint raw)
    {
        if (!writer.TryWrite(new(null, raw, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    private readonly record struct PacketDescriptor(IMqttPacket5 Packet, uint Raw, TaskCompletionSource Completion);
}