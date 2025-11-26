using System.Runtime.InteropServices;

namespace Net.Mqtt.Benchmarks.Extensions;

public static partial class TopicHelpersV13
{
    public static bool IsValidFilter(ReadOnlySpan<byte> filter)
    {
        if (filter.IsEmpty) return false;

        var lastIndex = filter.Length - 1;

        for (var i = 0; i < filter.Length; i++)
        {
            switch (filter[i])
            {
                case (byte)'+' when i > 0 && filter[i - 1] != '/' || i < lastIndex && filter[i + 1] != '/':
                case (byte)'#' when i != lastIndex || i > 0 && filter[i - 1] != '/':
                    return false;
            }
        }

        return true;
    }

    public static bool TopicMatches(ReadOnlySpan<byte> topic, ReadOnlySpan<byte> filter)
    {
        var topicLen = topic.Length;
        var filterLen = filter.Length;

        if (topicLen == 0 || filterLen == 0) return false;

        ref var topicRef = ref MemoryMarshal.GetReference(topic);
        ref var filterRef = ref MemoryMarshal.GetReference(filter);

        do
        {
            Debug.Assert(topicLen > 0, $"{nameof(topicLen)} cannot be 0 at this stage");
            Debug.Assert(filterLen > 0, $"{nameof(filterLen)} cannot be 0 at this stage");

            if (filterRef == topicRef)
            {
                var offset = CommonPrefixLength(ref topicRef, ref filterRef, topicLen < filterLen ? topicLen : filterLen);

                filterLen -= offset;
                topicLen -= offset;

                if (filterLen == 0) return topicLen == 0;

                filterRef = ref Unsafe.AddByteOffset(ref filterRef, offset);
                topicRef = ref Unsafe.AddByteOffset(ref topicRef, offset);
            }

            var b = filterRef;

            if (b == '+')
            {
                var offset = FirstSegmentLength(ref topicRef, topicLen);

                topicLen -= offset;
                topicRef = ref Unsafe.AddByteOffset(ref topicRef, offset);

                filterLen -= 1;
                filterRef = ref Unsafe.AddByteOffset(ref filterRef, 1);
            }
            else
            {
                return b == '#' || b == '/' && topicLen == 0 && filterLen == 2 && Unsafe.AddByteOffset(ref filterRef, 1) == '#';
            }

            if (topicLen == 0)
            {
                return filterLen == 0 || filterLen == 2 && filterRef == '/' && Unsafe.AddByteOffset(ref filterRef, 1) == '#';
            }
        } while (filterLen > 0);

        return false;
    }

    internal static int CommonPrefixLength(ref byte left, ref byte right, int length)
    {
        nuint i = 0;
        uint mask;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            do
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();
                if (mask != 0xFFFF_FFFFu) goto ret_add_mask_tzc;
                i += (nuint)Vector256<byte>.Count;
            } while ((nint)i < length - Vector256<byte>.Count);

            i = (nuint)length - (nuint)Vector256<byte>.Count;
            mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();
            if (mask != 0xFFFF_FFFFu) goto ret_add_mask_tzc;
            i += (nuint)Vector256<byte>.Count;
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            do
            {
                mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, i), Vector128.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();
                if (mask != 0xFFFFu) goto ret_add_mask_tzc;
                i += (nuint)Vector128<byte>.Count;
            } while ((nint)i < length - Vector128<byte>.Count);

            i = (nuint)length - (nuint)Vector128<byte>.Count;
            mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, i), Vector128.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();
            if (mask != 0xFFFFu) goto ret_add_mask_tzc;
            i += (nuint)Vector128<byte>.Count;
        }
        else if (length >= nuint.Size)
        {
            do
            {
                var x = BitXor<nuint>(ref left, ref right, i);
                if (x != 0)
                    return (int)i + (int)LeadingZeroByteCount(x);
                i += (nuint)nuint.Size;
            } while ((nint)i < (nint)length - nuint.Size);

            {
                i = (nuint)length - (nuint)nuint.Size;
                var x = BitXor<nuint>(ref left, ref right, i);
                if (x != 0)
                    return (int)i + (int)LeadingZeroByteCount(x);
                i += (nuint)nuint.Size;
            }
        }
        else
        {
            if (nuint.Size == sizeof(ulong) && length >= sizeof(uint))
            {
                var x = BitXor<uint>(ref left, ref right, i);
                if (x != 0)
                    return (int)LeadingZeroByteCount(x);
                i += sizeof(uint);
            }

            for (; (nint)i < length; i++)
            {
                if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i))
                    break;
            }
        }

        return (int)i;
    ret_add_mask_tzc:
        return (int)(i + uint.TrailingZeroCount(~mask));
    }

    internal static int FirstSegmentLength(ref byte source, int length)
    {
        const byte separator = (byte)'/';

        nuint i = 0;
        uint mask;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            do
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref source, i), Vector256.Create(separator)).ExtractMostSignificantBits();
                if (mask != 0x0u) goto ret_add_mask_tzc;
                i += (nuint)Vector256<byte>.Count;
            } while ((nint)i < length - Vector256<byte>.Count);

            i = (nuint)length - (nuint)Vector256<byte>.Count;
            mask = Vector256.Equals(Vector256.LoadUnsafe(ref source, i), Vector256.Create(separator)).ExtractMostSignificantBits();
            if (mask != 0x0u) goto ret_add_mask_tzc;
            i += (nuint)Vector256<byte>.Count;
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            do
            {
                mask = Vector128.Equals(Vector128.LoadUnsafe(ref source, i), Vector128.Create(separator)).ExtractMostSignificantBits();
                if (mask != 0x0u) goto ret_add_mask_tzc;
                i += (nuint)Vector128<byte>.Count;
            } while ((nint)i < length - Vector128<byte>.Count);

            i = (nuint)length - (nuint)Vector128<byte>.Count;
            mask = Vector128.Equals(Vector128.LoadUnsafe(ref source, i), Vector128.Create(separator)).ExtractMostSignificantBits();
            if (mask != 0x0u) goto ret_add_mask_tzc;
            i += (nuint)Vector128<byte>.Count;
        }
        else
        {
            // Add this extra check to prevent wrongly entering the loop due to extreme negative 
            // integer (int.MinValue e.g.) overflow in loop condition for 32-bit process 
            // (when nint and int are the same). Although negative length value should not happen in our 
            // code while this method is used internally, however let's better have this extra precaution.
            // Notice, this condition will be elided by the JIT on 64-bit process and will not affect performance
            // at all. It is safe to be removed on 64-bits, because loop condition will handle the situation safely
            // by upcasting negative int to a larger size signed nint which is definetely far away 
            // from overflow when substracting 4 from it
            if (nuint.Size == sizeof(ulong) || length >= 4)
            {
                for (; (nint)i <= (nint)length - 4; i += 4)
                {
                    if (Unsafe.AddByteOffset(ref source, i + 0) == separator) goto ret;
                    if (Unsafe.AddByteOffset(ref source, i + 1) == separator) return (int)i + 1;
                    if (Unsafe.AddByteOffset(ref source, i + 2) == separator) return (int)i + 2;
                    if (Unsafe.AddByteOffset(ref source, i + 3) == separator) return (int)i + 3;
                }
            }

            for (; (nint)i < length; i++)
            {
                if (Unsafe.AddByteOffset(ref source, i) == separator)
                    break;
            }
        }

    ret:
        return (int)i;
    ret_add_mask_tzc:
        return (int)(i + uint.TrailingZeroCount(mask));
    }

    [MethodImpl(AggressiveInlining)]
    private static T LeadingZeroByteCount<T>(T x) where T : struct, IBinaryInteger<T> =>
        (BitConverter.IsLittleEndian ? T.TrailingZeroCount(x) : T.LeadingZeroCount(x)) >>> 3;

    [MethodImpl(AggressiveInlining)]
    private static T BitXor<T>(ref byte left, ref byte right, nuint offset) where T : struct, IBitwiseOperators<T, T, T> =>
        Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref left, offset)) ^
        Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref right, offset));
}