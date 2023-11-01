using System.Buffers.Binary;

namespace System.Net.Mqtt.Client;

public partial class MqttClient5
{
    protected override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var descriptor))
            {
                stoppingToken.ThrowIfCancellationRequested();
                var tcs = descriptor.Completion;
                if (tcs is { Task.IsCompleted: true })
                    return;

                var (packet, raw, _) = descriptor;

                try
                {
                    if ((raw & 0xF000_0000) is not 0)
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(output.GetSpan(4), raw);
                        output.Advance(4);
                    }
                    else if (packet is not null)
                    {
                        packet.Write(output, int.MaxValue);
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
                        return;
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
}