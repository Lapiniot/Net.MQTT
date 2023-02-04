using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Net.Mqtt.Extensions;

public static class MqttExtensions
{
    [MethodImpl(AggressiveInlining)]
    public static int GetLengthByteCount(int length) => length is not 0 ? (int)Math.Log(length, 128) + 1 : 1;

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

    [MethodImpl(AggressiveInlining | AggressiveOptimization)]
    public static bool TopicMatches(ReadOnlySpan<byte> topic, ReadOnlySpan<byte> filter)
    {
        var t_len = topic.Length;
        var f_len = filter.Length;

        if (t_len == 0 || f_len == 0) return false;

        ref var t_ref = ref Unsafe.AsRef(in topic[0]);
        ref var f_ref = ref Unsafe.AsRef(in filter[0]);

        do
        {
            Debug.Assert(t_len > 0, "t_len cannot be 0 at this stage");
            Debug.Assert(f_len > 0, "f_len cannot be 0 at this stage");

            var length = t_len < f_len ? t_len : f_len;

            // Quick probablistic test to avoid expensive common prefix length computation 
            // if first chars already mismatch
            if (f_ref == t_ref)
            {
                var offset = CommonPrefixLength(ref t_ref, ref f_ref, length);

                t_len -= offset;
                f_len -= offset;
                t_ref = ref Unsafe.Add(ref t_ref, offset);
                f_ref = ref Unsafe.Add(ref f_ref, offset);

                if (f_len == 0) return t_len == 0;
            }

            var b = f_ref;

            if (b == '+')
            {
                f_ref = ref Unsafe.Add(ref f_ref, 1);
                f_len--;

                var offset = FirstSegmentLength(ref t_ref, t_len);

                t_ref = ref Unsafe.Add(ref t_ref, offset);
                t_len -= offset;
            }
            else
            {
                return b == '#' || b == '/' && t_len == 0 && f_len == 2 && Unsafe.Add(ref f_ref, 1) == '#';
            }

            if (t_len == 0)
            {
                return f_len == 0 || f_len == 2 && f_ref == '/' && Unsafe.Add(ref f_ref, 1) == '#';
            }
        } while (f_len > 0 || t_len > 0);

        return false;
    }

    [MethodImpl(AggressiveInlining)]
    internal static int CommonPrefixLength(ref byte left, ref byte right, int length)
    {
        nuint index = 0;
        uint mask;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            //hardware SIMD256 instructions are supported and data is large enough:
            var boundary = (nuint)(length - Vector256<byte>.Count);

            for (; length > Vector256<byte>.Count; index += (nuint)Vector256<byte>.Count, length -= Vector256<byte>.Count)
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, index), Vector256.LoadUnsafe(ref right, index)).ExtractMostSignificantBits();

                if (mask != 0xFFFF_FFFF) goto ret_add_mask_tzc;
            }

            index = boundary;

            mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, index), Vector256.LoadUnsafe(ref right, index)).ExtractMostSignificantBits();

            if (mask != 0xFFFF_FFFF) goto ret_add_mask_tzc;

            index += (nuint)Vector256<byte>.Count;
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            //hardware SIMD128 instructions are supported and data is large enough:
            var boundary = (nuint)(length - Vector128<byte>.Count);

            for (; length > Vector128<byte>.Count; index += (nuint)Vector128<byte>.Count, length -= Vector128<byte>.Count)
            {
                mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, index), Vector128.LoadUnsafe(ref right, index)).ExtractMostSignificantBits();

                if (mask != 0xFFFF) goto ret_add_mask_tzc;
            }

            index = boundary;

            mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, index), Vector128.LoadUnsafe(ref right, index)).ExtractMostSignificantBits();

            if (mask != 0xFFFF) goto ret_add_mask_tzc;

            index += (nuint)Vector128<byte>.Count;
        }
        else
        {
            for (; length >= 4; length -= 4, index += 4)
            {
                if (Unsafe.Add(ref left, index) != Unsafe.Add(ref right, index)) goto ret;
                if (Unsafe.Add(ref left, index + 1) != Unsafe.Add(ref right, index + 1)) goto ret_add_1;
                if (Unsafe.Add(ref left, index + 2) != Unsafe.Add(ref right, index + 2)) goto ret_add_2;
                if (Unsafe.Add(ref left, index + 3) != Unsafe.Add(ref right, index + 3)) goto ret_add_3;
            }

            for (; length > 0; length--, index++)
            {
                if (Unsafe.Add(ref left, index) != Unsafe.Add(ref right, index)) goto ret;
            }
        }

    ret:
        return (int)index;
    ret_add_1:
        return (int)(index + 1);
    ret_add_2:
        return (int)(index + 2);
    ret_add_3:
        return (int)(index + 3);
    ret_add_mask_tzc:
        return (int)(index + uint.TrailingZeroCount(~mask));
    }

    [MethodImpl(AggressiveInlining)]
    internal static int FirstSegmentLength(ref byte source, int length)
    {
        const byte value = 0x2f;

        nuint index = 0;
        uint mask;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            for (; length >= Vector256<byte>.Count; index += (nuint)Vector256<byte>.Count, length -= Vector256<byte>.Count)
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref source, index), Vector256.Create(value)).ExtractMostSignificantBits();

                if (mask != 0x0) goto ret_add_mask_tzc;
            }
        }

        if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            for (; length >= Vector128<byte>.Count; index += (nuint)Vector128<byte>.Count, length -= Vector128<byte>.Count)
            {
                mask = Vector128.Equals(Vector128.LoadUnsafe(ref source, index), Vector128.Create(value)).ExtractMostSignificantBits();

                if (mask != 0x0) goto ret_add_mask_tzc;
            }
        }

        for (; length >= 4; index += 4, length -= 4)
        {
            if (Unsafe.Add(ref source, index) == value) goto ret;
            if (Unsafe.Add(ref source, index + 1) == value) goto ret_add_1;
            if (Unsafe.Add(ref source, index + 2) == value) goto ret_add_2;
            if (Unsafe.Add(ref source, index + 3) == value) goto ret_add_3;
        }

        for (; length > 0; index++, length--)
        {
            if (Unsafe.Add(ref source, index) == value) goto ret;
        }

    ret:
        return (int)index;
    ret_add_1:
        return (int)(index + 1);
    ret_add_2:
        return (int)(index + 2);
    ret_add_3:
        return (int)(index + 3);
    ret_add_mask_tzc:
        return (int)(index + uint.TrailingZeroCount(mask));
    }
}