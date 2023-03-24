using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Properties;

namespace System.Net.Mqtt.Client;

public abstract class MqttClientProtocol : MqttProtocol
{
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;

    protected internal MqttClientProtocol(NetworkTransportPipe transport, bool disposeTransport)
        : base(transport, disposeTransport)
    { }

    public abstract byte ProtocolLevel { get; }

    public abstract string ProtocolName { get; }

    protected void Post(MqttPacket packet, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(packet, null, default, 0, completion)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer.TryWrite(new(null, null, default, value, null)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)), completion)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected sealed override async Task RunPacketDispatcherAsync(CancellationToken stoppingToken)
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
                            WritePublishPacket(output, (byte)(raw & 0xff), (ushort)(raw >> 8), topic, payload);
                        }
                        else if (raw > 0)
                        {
                            // Simple packet 4 or 2 bytes in size
                            WriteRawPacket(output, raw);
                        }
                        else if (packet is not null)
                        {
                            // Reference to any generic packet implementation
                            WriteGenericPacket(output, packet);
                        }
                        else
                        {
                            ThrowInvalidDispatchBlock();
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

    protected sealed override void InitPacketDispatcher() => (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void CompletePacketDispatch() => writer.Complete();

    [DoesNotReturn]
    protected static void ThrowInvalidDispatchBlock() =>
        throw new InvalidOperationException(Strings.InvalidDispatchBlockData);

    [DoesNotReturn]
    protected static void ThrowInvalidConnAckPacket() =>
        throw new InvalidDataException("Invalid CONNECT response. Valid CONNACK packet expected.");

    [DoesNotReturn]
    protected static void ThrowCannotWriteToQueue() =>
        throw new InvalidOperationException(Strings.CannotAddOutgoingPacket);

    private record struct DispatchBlock(MqttPacket Packet, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Buffer, uint Raw, TaskCompletionSource Completion);
}