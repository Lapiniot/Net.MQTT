using System.Buffers;
using System.Buffers.Binary;
using System.Net.Connections.Exceptions;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Properties;
using System.Threading.Channels;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server;

public abstract class MqttServerProtocol : MqttProtocol
{
    private readonly byte[] rawBuffer = new byte[4];
    private ChannelReader<DispatchBlock> reader;
    private ChannelWriter<DispatchBlock> writer;

    protected internal MqttServerProtocol(NetworkTransport transport, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        this[Connect] = OnConnect;
        this[Subscribe] = OnSubscribe;
        this[Unsubscribe] = OnUnsubscribe;
        this[PingReq] = OnPingReq;
        this[Disconnect] = OnDisconnect;
    }

    protected abstract void OnConnect(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnSubscribe(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnUnsubscribe(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnPingReq(byte header, ReadOnlySequence<byte> reminder);

    protected abstract void OnDisconnect(byte header, ReadOnlySequence<byte> reminder);

    protected void Post(MqttPacket packet)
    {
        if (!writer.TryWrite(new(packet, null, default, 0)))
        {
            throw new InvalidOperationException(Strings.CannotAddOutgoingPacket);
        }
    }

    protected void Post(byte[] bytes)
    {
        if (!writer.TryWrite(new(null, null, bytes, 0)))
        {
            throw new InvalidOperationException(Strings.CannotAddOutgoingPacket);
        }
    }

    protected void Post(uint value)
    {
        if (!writer.TryWrite(new(null, null, default, value)))
        {
            throw new InvalidOperationException(Strings.CannotAddOutgoingPacket);
        }
    }

    protected void PostPublish(byte flags, ushort id, string topic, in ReadOnlyMemory<byte> payload)
    {
        if (!writer.TryWrite(new(null, topic, in payload, (uint)(flags | (id << 8)))))
        {
            throw new InvalidOperationException(Strings.CannotAddOutgoingPacket);
        }
    }

    protected sealed override async Task RunPacketDispatcherAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var (packet, topic, payload, raw) = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);

                try
                {
                    if (topic is not null)
                    {
                        // Decomposed PUBLISH packet
                        var flags = (byte)(raw & 0xff);
                        var id = (ushort)(raw >> 8);

                        var total = PublishPacket.GetSize(flags, topic, payload, out var remainingLength);
                        var buffer = ArrayPool<byte>.Shared.Rent(total);

                        try
                        {
                            PublishPacket.Write(buffer, remainingLength, flags, id, topic, payload.Span);
                            await Transport.SendAsync(buffer.AsMemory(0, total), stoppingToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                    else if (raw > 0)
                    {
                        // Simple packet with id (4 bytes in size exactly)
                        BinaryPrimitives.WriteUInt32BigEndian(rawBuffer, raw);
                        await Transport.SendAsync(rawBuffer, stoppingToken).ConfigureAwait(false);
                    }
                    else if (payload is { Length: > 0 })
                    {
                        // Pre-composed buffer with complete packet data
                        await Transport.SendAsync(payload, stoppingToken).ConfigureAwait(false);
                    }
                    else if (packet is not null)
                    {
                        // Reference to any generic packet implementation
                        var total = packet.GetSize(out var remainingLength);

                        var buffer = ArrayPool<byte>.Shared.Rent(total);

                        try
                        {
                            packet.Write(buffer, remainingLength);
                            await Transport.SendAsync(buffer.AsMemory(0, total), stoppingToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(Strings.InvalidDispatchBlockData);
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
    }

    protected sealed override void InitPacketDispatcher() => (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void CompletePacketDispatch() => writer.Complete();

    protected static void ThrowInvalidSubscribePacket() => throw new InvalidDataException(Strings.InvalidSubscribePacket);

    private record struct DispatchBlock(MqttPacket Packet, string Topic, in ReadOnlyMemory<byte> Buffer, uint Raw);
}