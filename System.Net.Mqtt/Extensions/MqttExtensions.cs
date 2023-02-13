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

    [MethodImpl(AggressiveInlining)]
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
                t_ref = ref Unsafe.AddByteOffset(ref t_ref, offset);
                f_ref = ref Unsafe.AddByteOffset(ref f_ref, offset);

                if (f_len == 0) return t_len == 0;
            }

            var b = f_ref;

            if (b == '+')
            {
                f_ref = ref Unsafe.AddByteOffset(ref f_ref, 1);
                f_len--;

                var offset = FirstSegmentLength(ref t_ref, t_len);

                t_ref = ref Unsafe.AddByteOffset(ref t_ref, offset);
                t_len -= offset;
            }
            else
            {
                return b == '#' || b == '/' && t_len == 0 && f_len == 2 && Unsafe.AddByteOffset(ref f_ref, 1) == '#';
            }

            if (t_len == 0)
            {
                return f_len == 0 || f_len == 2 && f_ref == '/' && Unsafe.AddByteOffset(ref f_ref, 1) == '#';
            }
        } while (f_len > 0);

        return false;
    }

    [MethodImpl(AggressiveInlining)]
    internal static int CommonPrefixLength(ref byte left, ref byte right, int length)
    {
        nuint i = 0;
        uint mask;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            //hardware SIMD256 instructions are supported and data is large enough:
            var oneFromEndOffset = (nuint)(length - Vector256<byte>.Count);

            for (; i < oneFromEndOffset; i += (nuint)Vector256<byte>.Count)
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

                if (mask != 0xFFFF_FFFF) goto ret_add_mask_tzc;
            }

            i = oneFromEndOffset;

            mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

            if (mask != 0xFFFF_FFFF) goto ret_add_mask_tzc;

            i += (nuint)Vector256<byte>.Count;
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            //hardware SIMD128 instructions are supported and data is large enough:
            var oneFromEndOffset = (nuint)(length - Vector128<byte>.Count);

            for (; i < oneFromEndOffset; i += (nuint)Vector128<byte>.Count)
            {
                mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, i), Vector128.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

                if (mask != 0xFFFF) goto ret_add_mask_tzc;
            }

            i = oneFromEndOffset;

            mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, i), Vector128.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

            if (mask != 0xFFFF) goto ret_add_mask_tzc;

            i += (nuint)Vector128<byte>.Count;
        }
        else
        {
            var boundary = length - 4;
            for (; (int)i <= boundary; i += 4)
            {
                if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i))
                    return (int)i;
                if (Unsafe.AddByteOffset(ref left, i + 1) != Unsafe.AddByteOffset(ref right, i + 1))
                    return (int)(i + 1);
                if (Unsafe.AddByteOffset(ref left, i + 2) != Unsafe.AddByteOffset(ref right, i + 2))
                    return (int)(i + 2);
                if (Unsafe.AddByteOffset(ref left, i + 3) != Unsafe.AddByteOffset(ref right, i + 3))
                    return (int)(i + 3);
            }

            for (; i < (nuint)length; i++)
            {
                if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i))
                    break;
            }
        }

        return (int)i;
    ret_add_mask_tzc:
        return (int)(i + uint.TrailingZeroCount(~mask));
    }

    [MethodImpl(AggressiveInlining)]
    internal static int FirstSegmentLength(ref byte source, int length)
    {
        const byte v = 0x2f;
        var v32v = Vector256.Create(v);
        var v16v = Vector128.Create(v);

        nuint i = 0;
        uint mask;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            //hardware SIMD256 instructions are supported and data is large enough:
            var oneFromEndOffset = (nuint)(length - Vector256<byte>.Count);

            for (; i < oneFromEndOffset; i += (nuint)Vector256<byte>.Count)
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref source, i), v32v).ExtractMostSignificantBits();

                if (mask != 0x0) goto ret_add_mask_tzc;
            }

            i = oneFromEndOffset;

            mask = Vector256.Equals(Vector256.LoadUnsafe(ref source, i), v32v).ExtractMostSignificantBits();

            if (mask != 0x0) goto ret_add_mask_tzc;

            i += (nuint)Vector256<byte>.Count;
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            //hardware SIMD128 instructions are supported and data is large enough:
            var oneFromEndOffset = (nuint)(length - Vector128<byte>.Count);

            for (; i < oneFromEndOffset; i += (nuint)Vector128<byte>.Count)
            {
                mask = Vector128.Equals(Vector128.LoadUnsafe(ref source, i), v16v).ExtractMostSignificantBits();

                if (mask != 0x0) goto ret_add_mask_tzc;
            }

            i = oneFromEndOffset;

            mask = Vector128.Equals(Vector128.LoadUnsafe(ref source, i), v16v).ExtractMostSignificantBits();

            if (mask != 0x0) goto ret_add_mask_tzc;

            i += (nuint)Vector128<byte>.Count;
        }
        else
        {
            var boundary = length - 4;
            for (; (int)i <= boundary; i += 4)
            {
                if (Unsafe.AddByteOffset(ref source, i) == v)
                    return (int)i;
                if (Unsafe.AddByteOffset(ref source, i + 1) == v)
                    return (int)(i + 1);
                if (Unsafe.AddByteOffset(ref source, i + 2) == v)
                    return (int)(i + 2);
                if (Unsafe.AddByteOffset(ref source, i + 3) == v)
                    return (int)(i + 3);
            }

            for (; i < (nuint)length; i++)
            {
                if (Unsafe.AddByteOffset(ref source, i) == v)
                    break;
            }
        }

        return (int)i;
    ret_add_mask_tzc:
        return (int)(i + uint.TrailingZeroCount(mask));
    }
}