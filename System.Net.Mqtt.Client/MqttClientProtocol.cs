using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Properties;
using static System.Net.Mqtt.PacketType;

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

    protected sealed override void Dispatch(PacketType type, byte flags, in ReadOnlySequence<byte> reminder)
    {
        // CLR JIT will generate efficient jump table for this switch statement, 
        // as soon as case patterns are incuring constant number values ordered in the following way
        switch (type)
        {
            case ConnAck: OnConnAck(flags, in reminder); break;
            case Publish: OnPublish(flags, in reminder); break;
            case PubAck: OnPubAck(flags, in reminder); break;
            case PubRec: OnPubRec(flags, in reminder); break;
            case PubRel: OnPubRel(flags, in reminder); break;
            case PubComp: OnPubComp(flags, in reminder); break;
            case SubAck: OnSubAck(flags, in reminder); break;
            case UnsubAck: OnUnsubAck(flags, in reminder); break;
            case PingResp: OnPingResp(flags, in reminder); break;
            default: MqttPacketHelpers.ThrowUnexpectedType((byte)type); break;
        }
    }

    protected abstract void OnConnAck(byte header, in ReadOnlySequence<byte> reminder);

    protected abstract void OnSubAck(byte header, in ReadOnlySequence<byte> reminder);

    protected abstract void OnUnsubAck(byte header, in ReadOnlySequence<byte> reminder);

    protected abstract void OnPingResp(byte header, in ReadOnlySequence<byte> reminder);

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