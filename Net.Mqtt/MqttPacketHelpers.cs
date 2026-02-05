using static System.Globalization.CultureInfo;
using SequenceExtensions = Net.Mqtt.Extensions.SequenceExtensions;

#pragma warning disable CA1034 // Nested types should not be visible

namespace Net.Mqtt;

/// <summary>
/// Provides MQTT packet related helpers.
/// </summary>
public static class MqttPacketHelpers
{
    extension<TPacket>(TPacket packet) where TPacket : IMqttPacket
    {
        /// <summary>
        /// Dumps packet bytes formatted as C# collection init expression via <see cref="Debug.WriteLine(string?)"/>
        /// </summary>
        [Conditional("DEBUG")]
        public void DebugDump()
        {
            ArgumentNullException.ThrowIfNull(packet);
            var writer = new ArrayBufferWriter<byte>();
            var written = packet.Write(writer);
            Debug.WriteLine($"[{string.Join(", ", writer.WrittenSpan.ToArray().Select(b => "0x" + b.ToString("x2", InvariantCulture)))}]");
        }
    }

    /// <summary>
    /// Asynchronously reads MQTT packet data from <see cref="PipeReader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="PipeReader"/> to read packet data from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to signal about cancellation.</param>
    /// <returns>A <see cref="PacketReadResult"/> containing fixed header information + entire 
    /// packet bytes as <see cref="ReadOnlySequence{T}"/> buffer fragment.</returns>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="MalformedPacketException" />
    /// <exception cref="OperationCanceledException" />
    /// <remarks>The caller is in charge to call <see cref="PipeReader.AdvanceTo(SequencePosition)"/> in order 
    /// to complete pipe read operation and advance to the next packet position</remarks>
    public static async ValueTask<PacketReadResult> ReadPacketAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            try
            {
                if (SequenceExtensions.TryReadMqttHeader(in buffer, out var header,
                    out var remainingLength, out var fixedHeaderLength))
                {
                    var total = fixedHeaderLength + remainingLength;
                    if (buffer.Length < total)
                    {
                        reader.AdvanceTo(consumed: buffer.Start, examined: buffer.Start);
                        result = await reader.ReadAtLeastAsync(total, cancellationToken).ConfigureAwait(false);
                        buffer = result.Buffer;
                    }

                    // We return packet header read result right away, but do not advance the reader itself.
                    // This is solely responsibility of the caller, and it is free to decide how to advance:
                    // - Either call reader.Advance(buffer.End, buffer.End) to mark 
                    // data as read and advance to the next packet position
                    // - Or call reader.Advance(buffer.Start, buffer.Start) to effectively cancel read 
                    // and unwind to the data strting position as a way to simulate PeekPacketAsync behavior e.g.
                    return new(header, fixedHeaderLength, remainingLength, buffer.Slice(0, total));
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

/// <summary>
/// Represents result of the <see cref="MqttPacketHelpers.ReadPacketAsync(PipeReader, CancellationToken)"/> call.
/// </summary>
/// <param name="ControlHeader">A fixed header's control header value (packet type (4 bits) + packet flags (4 bit)).</param>
/// <param name="FixedHeaderLength">A fixed header overall length (control header (1 byte) + remaining 
/// length field (variable length encoded 1-4 bytes)).</param>
/// <param name="RemainingLength">The value of a remaining length field.</param>
/// <param name="Buffer">The <see cref="ReadOnlySequence{T}"/> containing the data of 
/// entire packet (including fixed header, variable header and payload).</param>
public readonly record struct PacketReadResult(byte ControlHeader,
    int FixedHeaderLength, int RemainingLength, ReadOnlySequence<byte> Buffer);