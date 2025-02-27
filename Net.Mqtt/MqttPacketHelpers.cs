using static System.Globalization.CultureInfo;
using SequenceExtensions = Net.Mqtt.Extensions.SequenceExtensions;

namespace Net.Mqtt;

public static class MqttPacketHelpers
{
    [Conditional("DEBUG")]
    public static void DebugDump<TPacket>(this TPacket packet) where TPacket : IMqttPacket
    {
        ArgumentNullException.ThrowIfNull(packet);
        var writer = new ArrayBufferWriter<byte>();
        var written = packet.Write(writer);
        Debug.WriteLine($"{{{string.Join(",", writer.WrittenSpan.ToArray().Select(b => "0x" + b.ToString("x2", InvariantCulture)))}}}");
    }

    public static async ValueTask<PacketReadResult> ReadPacketAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            try
            {
                if (SequenceExtensions.TryReadMqttHeader(in buffer, out var flags, out var length, out var offset))
                {
                    var total = offset + length;
                    if (buffer.Length >= total)
                    {
                        // We return packet header read result right away, but do not advance the reader itself.
                        // This is solely responsibility of the caller, and it is free to decide how to advance:
                        // - Either call reader.Advance(buffer.End, buffer.End) to mark 
                        // data as read and advance to the next packet position
                        // - Or call reader.Advance(buffer.Start, buffer.Start) to effectively cancel read 
                        // and unwind to the data strting position as a way to simulate PeekPacketAsync behavior e.g.
                        return new(flags, offset, length, buffer.Slice(0, total));
                    }
                }
                else if (result.IsCompleted || buffer.Length >= 5)
                {
                    // We must stop and throw exception here, because there was no valid MQTT packet header
                    // found within 5 bytes (max possible fixed header size) already available or partner side 
                    // just completed writing and no more data is expected in the pipe.
                    MalformedPacketException.Throw();
                }

                reader.AdvanceTo(consumed: buffer.Start, examined: buffer.Start);
            }
            catch
            {
                // Unwind to the buffer start position effectivelly cancelling 
                // current read operation in case of any error.
                reader.AdvanceTo(consumed: buffer.Start, examined: buffer.Start);
                throw;
            }
        }
    }
}

public readonly record struct PacketReadResult(byte Flags, int Offset, int Length, ReadOnlySequence<byte> Buffer);