using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt;

public static class MqttPacketHelpers
{
    [Conditional("DEBUG")]
    public static void DebugDump(this MqttPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        var buffer = new byte[packet.GetSize(out var remainingLength)];
        packet.Write(buffer, remainingLength);
        Debug.WriteLine($"{{{string.Join(",", buffer.Select(b => "0x" + b.ToString("x2", InvariantCulture)))}}}");
    }

    public static async ValueTask<PacketReadResult> ReadPacketAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (SequenceExtensions.TryReadMqttHeader(in buffer, out var flags, out var length, out var offset))
            {
                var total = offset + length;
                if (buffer.Length >= total)
                {
                    return new(flags, offset, length, buffer.Slice(0, total));
                }
            }
            else if (buffer.Length >= 5)
            {
                // We must stop here, because no valid MQTT packet header
                // was found within 5 (max possible header size) bytes
                throw new InvalidDataException(InvalidDataStream);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }
}

public readonly record struct PacketReadResult(byte Flags, int Offset, int Length, ReadOnlySequence<byte> Buffer);