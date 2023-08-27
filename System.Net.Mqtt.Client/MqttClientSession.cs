using System.IO.Pipelines;
using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Client;

public abstract class MqttClientSession : MqttSession
{
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;

    protected internal MqttClientSession(NetworkTransportPipe transport, bool disposeTransport)
        : base(transport, disposeTransport)
    { }

    public abstract byte ProtocolLevel { get; }

    public abstract string ProtocolName { get; }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });
        return base.StartingAsync(cancellationToken);
    }

    protected override Task StoppingAsync()
    {
        writer.Complete();
        return base.StoppingAsync();
    }

    protected void Post(IMqttPacket packet, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(packet, null, default, 0, completion)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer.TryWrite(new(null, null, default, value, null)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            ThrowHelpers.ThrowCannotWriteToQueue();
        }
    }

    protected sealed override async Task RunProducerAsync(CancellationToken stoppingToken)
    {
        var output = Transport.Output;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var block))
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    var (packet, topic, payload, raw, tcs) = block;

                    try
                    {
                        if (tcs is { Task.IsCompleted: true }) return;

                        if (!topic.IsEmpty)
                        {
                            var flags = (byte)(raw & 0xff);
                            var size = PublishPacket.GetSize(flags, topic.Length, payload.Length, out var remainingLength);
                            PublishPacket.Write(output.GetSpan(size), remainingLength, flags, (ushort)(raw >> 8), topic.Span, payload.Span);
                            output.Advance(size);
                        }
                        else if (raw > 0)
                        {
                            // Simple packet 4 or 2 bytes in size
                            if ((raw & 0xFF00_0000) > 0)
                            {
                                WritePacket(output, raw);
                            }
                            else
                            {
                                WritePacket(output, (ushort)raw);
                            }
                        }
                        else if (packet is not null)
                        {
                            // Reference to any generic packet implementation
                            WritePacket(output, packet, out _, out _);
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
                    catch (ConnectionClosedException cce)
                    {
                        tcs?.TrySetException(cce);
                        break;
                    }
                    catch (Exception ex)
                    {
                        tcs?.TrySetException(ex);
                        throw;
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
        }
    }

    private record struct DispatchBlock(IMqttPacket Packet, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Buffer, uint Raw, TaskCompletionSource Completion);
}