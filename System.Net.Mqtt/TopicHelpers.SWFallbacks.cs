namespace System.Net.Mqtt;

public static partial class TopicHelpers
{
    internal static int CommonPrefixLengthScalar(ref byte left, ref byte right, int length)
    {
        nuint i = 0;

        if (length >= 4)
        {
            var l = (nuint)(length - 4);
            for (; i <= l; i += 4)
            {
                if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i)) return (int)i;
                if (Unsafe.AddByteOffset(ref left, i + 1) != Unsafe.AddByteOffset(ref right, i + 1)) return (int)(i + 1);
                if (Unsafe.AddByteOffset(ref left, i + 2) != Unsafe.AddByteOffset(ref right, i + 2)) return (int)(i + 2);
                if (Unsafe.AddByteOffset(ref left, i + 3) != Unsafe.AddByteOffset(ref right, i + 3)) return (int)(i + 3);
            }
        }

        for (; (int)i < length; i++)
        {
            if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i))
            {
                break;
            }
        }

        return (int)i;
    }

    internal static int CommonPrefixLengthSWAR(ref byte left, ref byte right, int length)
    {
        nuint i = 0;

        for (; (int)i <= length - nuint.Size; i += (nuint)nuint.Size)
        {
            var x = Unsafe.As<byte, nuint>(ref Unsafe.AddByteOffset(ref left, i)) ^ Unsafe.As<byte, nuint>(ref Unsafe.AddByteOffset(ref right, i));
            if (x != 0)
            {
                var zeroBitsCount = BitConverter.IsLittleEndian ? BitOperations.TrailingZeroCount(x) : BitOperations.LeadingZeroCount(x);
                return (int)i + (zeroBitsCount >> 3);
            }
        }

        for (; (int)i < length; i++)
        {
            if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i))
            {
                break;
            }
        }

        return (int)i;
    }

    internal static int FirstSegmentLengthScalar(ref byte source, int length)
    {
        const byte value = 0x2f;
        nuint i = 0;

        if (length >= 4)
        {
            for (; (int)i <= length - 4; i += 4)
            {
                if (Unsafe.AddByteOffset(ref source, i) == value) goto ret;
                if (Unsafe.AddByteOffset(ref source, i + 1) == value) return (int)(i + 1);
                if (Unsafe.AddByteOffset(ref source, i + 2) == value) return (int)(i + 2);
                if (Unsafe.AddByteOffset(ref source, i + 3) == value) return (int)(i + 3);
            }
        }

        for (; (int)i < length; i++)
        {
            if (Unsafe.AddByteOffset(ref source, i) == value) break;
        }

    ret:
        return (int)i;
    }

    internal static int FirstSegmentLengthSWAR(ref byte source, int length)
    {
        const byte value = 0x2f;

        var lowBitMask = ~(nuint)0 / 255;       // 0x0101010101010101
        var highBitMask = 0x80 * lowBitMask;    // 0x8080808080808080
        var valueMask = value * lowBitMask;     // 0x2f2f2f2f2f2f2f2f

        nuint i = 0;

        for (; (int)i <= length - nuint.Size; i += (nuint)nuint.Size)
        {
            var x = Unsafe.As<byte, nuint>(ref Unsafe.AddByteOffset(ref source, i)) ^ valueMask;
            x = (x - lowBitMask) & ~x & highBitMask;
            if (x != 0)
            {
                var zeroBitsCount = BitConverter.IsLittleEndian ? BitOperations.TrailingZeroCount(x) : BitOperations.LeadingZeroCount(x);
                return (int)i + (zeroBitsCount >> 3);
            }
        }

        for (; (int)i < length; i++)
        {
            if (Unsafe.AddByteOffset(ref source, i) == value)
            {
                break;
            }
        }

        return (int)i;
    }
}