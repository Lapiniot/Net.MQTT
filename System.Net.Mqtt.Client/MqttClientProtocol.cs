using System.Buffers.Binary;
using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Properties;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Client;

public abstract class MqttClientProtocol : MqttProtocol
{
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;

    protected internal MqttClientProtocol(NetworkTransport transport, bool disposeTransport)
        : base(transport, disposeTransport)
    {
        this[ConnAck] = OnConnAck;
        this[SubAck] = OnSubAck;
        this[UnsubAck] = OnUnsubAck;
        this[PingResp] = OnPingResp;
    }

    public abstract byte ProtocolLevel { get; }

    public abstract string ProtocolName { get; }

    protected abstract void OnConnAck(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnSubAck(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnUnsubAck(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnPingResp(byte header, ReadOnlySequence<byte> reminder);

    protected void Post(MqttPacket packet, TaskCompletionSource completion = null)
    {
        if (!writer.TryWrite(new(packet, null, default, 0, completion)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(byte[] bytes)
    {
        if (!writer.TryWrite(new(null, null, bytes, 0, null)))
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
                            // Decomposed PUBLISH packet
                            var flags = (byte)(raw & 0xff);
                            var id = (ushort)(raw >> 8);

                            var total = PublishPacket.GetSize(flags, topic.Length, payload.Length, out var remainingLength);
                            var buffer = output.GetMemory(total);

                            PublishPacket.Write(buffer.Span, remainingLength, flags, id, topic.Span, payload.Span);
                            output.Advance(total);
                            await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                        }
                        else if (raw > 0)
                        {
                            var buffer = output.GetMemory(4);
                            // Simple packet with id (4 bytes in size exactly)
                            BinaryPrimitives.WriteUInt32BigEndian(buffer.Span, raw);
                            output.Advance(4);
                            await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                        }
                        else if (payload is { Length: > 0 })
                        {
                            /*
                            TODO: Seems this branch is only used for 2-byte length PingResp packets, 
                            consider switch to simple bit-packing into DispatchBlock.Raw field 
                            */

                            // Pre-composed buffer with complete packet data
                            await output.WriteAsync(payload, stoppingToken).ConfigureAwait(false);
                        }
                        else if (packet is not null)
                        {
                            // Reference to any generic packet implementation
                            var total = packet.GetSize(out var remainingLength);
                            var buffer = output.GetMemory(total);

                            packet.Write(buffer.Span, remainingLength);
                            output.Advance(total);
                            await output.FlushAsync(stoppingToken).ConfigureAwait(false);
                        }
                        else
                        {
                            ThrowInvalidDispatchBlock();
                        }

                        tcs?.TrySetResult();
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