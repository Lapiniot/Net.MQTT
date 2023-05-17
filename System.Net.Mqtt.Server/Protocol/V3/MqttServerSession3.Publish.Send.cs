using System.Net.Mqtt.Packets.V3;
using SequenceExtensions = System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    private async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        var reader = state!.OutgoingReader;

        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (reader.TryPeek(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var (topic, payload, qos, _) = message;

                switch (qos)
                {
                    case 0:
                        PostPublish(0, 0, topic, in payload);
                        break;

                    case 1:
                    case 2:
                        var flags = (byte)(qos << 1);
                        var id = await state.CreateMessageDeliveryStateAsync(flags, topic, payload, stoppingToken).ConfigureAwait(false);
                        PostPublish(flags, id, topic, in payload);
                        break;

                    default:
                        InvalidQoSException.Throw();
                        break;
                }

                reader.TryRead(out _);
            }
        }
    }

    protected sealed override async Task RunPacketDispatcherAsync(CancellationToken stoppingToken)
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
                    var (packet, topic, payload, raw) = block;

                    try
                    {
                        if (!topic.IsEmpty)
                        {
                            // Decomposed PUBLISH packet
                            var flags = (byte)(raw & 0xff);
                            var size = PublishPacket.GetSize(flags, topic.Length, payload.Length, out var remainingLength);
                            var buffer = output.GetMemory(size);
                            PublishPacket.Write(buffer.Span, remainingLength, flags, (ushort)(raw >> 8), topic.Span, payload.Span);
                            output.Advance(size);
                            OnPacketSent(0b0011, size);
                        }
                        else if (raw > 0)
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
            }

            result = await output.FlushAsync(stoppingToken).ConfigureAwait(false);
            if (result.IsCompleted || result.IsCanceled)
                return;
        }
    }

    protected sealed override void OnPacketDispatcherStartup() => (reader, writer) = Channel.CreateUnbounded<DispatchBlock>(new() { SingleReader = true, SingleWriter = false });

    protected sealed override void OnPacketDispatcherShutdown()
    {
        writer!.TryComplete();
        Transport.Output.CancelPendingFlush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubAck(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBACK");
        }

        state!.DiscardMessageDeliveryState(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRec(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREC");
        }

        state!.SetMessagePublishAcknowledged(id);
        Post(PacketFlags.PubRelPacketMask | id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubComp(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBCOMP");
        }

        state!.DiscardMessageDeliveryState(id);
    }

    protected void Post(MqttPacket packet)
    {
        if (!writer!.TryWrite(new(packet, null, default, 0)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void Post(uint value)
    {
        if (!writer!.TryWrite(new(null, null, default, value)))
        {
            ThrowCannotWriteToQueue();
        }
    }

    protected void PostPublish(byte flags, ushort id, ReadOnlyMemory<byte> topic, in ReadOnlyMemory<byte> payload)
    {
        if (!writer!.TryWrite(new(null, topic, payload, (uint)(flags | (id << 8)))))
        {
            ThrowCannotWriteToQueue();
        }
    }
}