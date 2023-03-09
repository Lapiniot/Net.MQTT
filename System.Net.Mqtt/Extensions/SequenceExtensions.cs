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

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out byte value)
    {
        if (sequence.First.Length > 0)
        {
            value = sequence.FirstSpan[0];
            return true;
        }

        foreach (var memory in sequence)
        {
            if (memory.Length == 0) continue;
            value = memory.Span[0];
            return true;
        }

        value = 0;
        return false;
    }

    public static bool TryReadMqttString(in ReadOnlySequence<byte> sequence, out byte[] value, out int consumed)
    {
        var span = sequence.FirstSpan;

        if (SpanExtensions.TryReadMqttString(in span, out value, out consumed))
        {
            return true;
        }

        if (!TryReadBigEndian(sequence, out var length) || length + 2 > sequence.Length)
        {
            return false;
        }

        var sliced = sequence.Slice(2, length);
        // TODO: try to use stackallock byte[length] for small strings (how long?)
        // use stackallock byte[sliced.Length] and sequentially copy all segments 
        // than convert to string
        value = sliced.ToArray();
        consumed = length + 2;
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