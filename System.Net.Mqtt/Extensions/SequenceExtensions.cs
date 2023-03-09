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
        var span = sequence.FirstSpan;

        if (SpanExtensions.TryReadMqttHeader(in span, out header, out length, out offset))
        {
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        if (SequenceReaderExtensions.TryReadMqttHeader(ref reader, out header, out length))
        {
            offset = (int)reader.Consumed;
            return true;
        }

        offset = 0;
        return false;
    }
}