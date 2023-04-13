using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Net.Mqtt.Extensions;

public static class SequenceExtensions
{
    public static bool TryReadBigEndian(in ReadOnlySequence<byte> sequence, out ushort value)
    {
        var position = sequence.Start;

        while (sequence.TryGet(ref position, out var memory, true))
        {
            var length = memory.Length;
            ref var source = ref MemoryMarshal.GetReference(memory.Span);

            if (length > 1)
            {
                value = Unsafe.ReadUnaligned<ushort>(ref source);
                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

                return true;
            }

            if (length == 1)
            {
                value = (ushort)(source << 8);
                while (sequence.TryGet(ref position, out memory, true))
                {
                    source = ref MemoryMarshal.GetReference(memory.Span);
                    if (memory.Length != 0)
                    {
                        value |= source;
                        return true;
                    }
                }

                break;
            }
        }

        value = 0;
        return false;
    }

    [MethodImpl(AggressiveInlining)]
    public static bool TryRead(in ReadOnlySequence<byte> sequence, out byte value)
    {
        var position = sequence.Start;
        while (sequence.TryGet(ref position, out var memory, true))
        {
            if (memory.Length != 0)
            {
                value = memory.Span[0];
                return true;
            }
        }

        value = 0;
        return false;
    }

    public static bool TryReadMqttString(in ReadOnlySequence<byte> sequence, out byte[] value, out int consumed)
    {
        if (!TryReadBigEndian(sequence, out var length) || length > sequence.Length - 2)
        {
            value = null;
            consumed = 0;
            return false;
        }

        value = new byte[length];
        sequence.Slice(2, length).CopyTo(value);
        consumed = 2 + length;
        return true;
    }

    public static bool TryReadMqttHeader(in ReadOnlySequence<byte> sequence, out byte header, out int length, out int offset)
    {
        var position = sequence.Start;
        while (sequence.TryGet(ref position, out var memory, true))
        {
            if (memory.Length == 0)
                continue;

            var span = memory.Span;
            length = 0;
            offset = 1;
            header = span[0];

            span = span.Slice(1);

            // The maximum number of bytes in the Remaining Length field is four.
            var maxBytesToRead = 4;
            var m = 1;

            while (true)
            {
                // read no more than allowed or span length (what is smaller)
                var limit = maxBytesToRead < span.Length ? maxBytesToRead : span.Length;

                for (var i = 0; i < limit; i++, m <<= 7, maxBytesToRead--)
                {
                    var x = span[i];
                    length += (x & 0b01111111) * m;
                    offset++;
                    if ((x & 0b10000000) == 0)
                        return true;
                }

                if (maxBytesToRead == 0 || !sequence.TryGet(ref position, out memory, true))
                    break;

                span = memory.Span;
            }

            break;
        }

        header = 0;
        length = 0;
        offset = 0;
        return false;
    }

    [Conditional("DEBUG")]
    public static void DebugDump(in ReadOnlySequence<byte> sequence) =>
        Debug.WriteLine($"{{{string.Join(",", sequence.ToArray().Select(b => "0x" + b.ToString("x2", CultureInfo.InvariantCulture)))}}}");
}