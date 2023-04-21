namespace System.Net.Mqtt.Extensions;

public static partial class MqttExtensions
{
    [MethodImpl(AggressiveInlining)]
    public static int GetLengthByteCount(int length) => BitOperations.Log2((uint)length) / 7 + 1;

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

    [MethodImpl(AggressiveOptimization)]
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

            // Quick probabilistic test to avoid expensive common prefix length computation 
            // if first chars already mismatch
            if (f_ref == t_ref)
            {
                var offset = CommonPrefixLength(ref t_ref, ref f_ref, length);

                f_len -= offset;
                t_len -= offset;

                if (f_len == 0) return t_len == 0;

                f_ref = ref Unsafe.AddByteOffset(ref f_ref, offset);
                t_ref = ref Unsafe.AddByteOffset(ref t_ref, offset);
            }

            var b = f_ref;

            if (b == '+')
            {
                var offset = FirstSegmentLength(ref t_ref, t_len);

                t_len -= offset;
                t_ref = ref Unsafe.AddByteOffset(ref t_ref, offset);

                f_len -= 1;
                f_ref = ref Unsafe.AddByteOffset(ref f_ref, 1);
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

            do
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

                if (mask != 0xFFFF_FFFFu) goto ret_add_mask_tzc;

                i += (nuint)Vector256<byte>.Count;
            } while (i < oneFromEndOffset);

            i = oneFromEndOffset;

            mask = Vector256.Equals(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

            if (mask != 0xFFFF_FFFFu) goto ret_add_mask_tzc;

            i += (nuint)Vector256<byte>.Count;
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            //hardware SIMD128 instructions are supported and data is large enough:
            var oneFromEndOffset = (nuint)(length - Vector128<byte>.Count);

            do
            {
                mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, i), Vector128.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

                if (mask != 0xFFFFu) goto ret_add_mask_tzc;

                i += (nuint)Vector128<byte>.Count;
            } while (i < oneFromEndOffset);

            i = oneFromEndOffset;

            mask = Vector128.Equals(Vector128.LoadUnsafe(ref left, i), Vector128.LoadUnsafe(ref right, i)).ExtractMostSignificantBits();

            if (mask != 0xFFFFu) goto ret_add_mask_tzc;

            i += (nuint)Vector128<byte>.Count;
        }
        else
        {
            for (; (int)i <= length - nuint.Size; i += (nuint)nuint.Size)
            {
                var x = Unsafe.As<byte, nuint>(ref Unsafe.AddByteOffset(ref left, i)) ^ Unsafe.As<byte, nuint>(ref Unsafe.AddByteOffset(ref right, i));
                if (x != 0)
                {
                    return (int)i + ((BitConverter.IsLittleEndian ? BitOperations.TrailingZeroCount(x) : BitOperations.LeadingZeroCount(x)) >> 3);
                }
            }

            if (nuint.Size == 8 && (int)i <= length - 4)
            {
                var x = Unsafe.As<byte, uint>(ref Unsafe.AddByteOffset(ref left, i)) ^ Unsafe.As<byte, uint>(ref Unsafe.AddByteOffset(ref right, i));
                if (x != 0)
                {
                    return (int)i + ((BitConverter.IsLittleEndian ? BitOperations.TrailingZeroCount(x) : BitOperations.LeadingZeroCount(x)) >> 3);
                }

                i += 4;
            }

            for (; (int)i < length; i++)
            {
                if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i))
                {
                    break;
                }
            }
        }

        return (int)i;
    ret_add_mask_tzc:
        return (int)(i + uint.TrailingZeroCount(~mask));
    }

    [MethodImpl(AggressiveInlining)]
    internal static int FirstSegmentLength(ref byte source, int length)
    {
        const byte value = 0x2f;

        nuint i = 0;
        uint mask;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            for (; length >= Vector256<byte>.Count; length -= Vector256<byte>.Count, i += (nuint)Vector256<byte>.Count)
            {
                mask = Vector256.Equals(Vector256.LoadUnsafe(ref source, i), Vector256.Create(value)).ExtractMostSignificantBits();

                if (mask != 0x0) goto ret_add_mask_tzc;
            }
        }

        if (Vector128.IsHardwareAccelerated)
        {
            if (length >= Vector128<byte>.Count)
            {
                for (; length >= Vector128<byte>.Count; length -= Vector128<byte>.Count, i += (nuint)Vector128<byte>.Count)
                {
                    mask = Vector128.Equals(Vector128.LoadUnsafe(ref source, i), Vector128.Create(value)).ExtractMostSignificantBits();

                    if (mask != 0x0) goto ret_add_mask_tzc;
                }
            }

            if (length >= sizeof(ulong))
            {
                mask = Vector128.Equals(
                    Vector128.CreateScalarUnsafe(Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref source, i))).AsByte(),
                    Vector128.Create(value)).ExtractMostSignificantBits();

                if (mask != 0x0) goto ret_add_mask_tzc;

                length -= sizeof(ulong);
                i += sizeof(ulong);
            }

            if (length >= sizeof(uint))
            {
                mask = Vector128.Equals(
                    Vector128.CreateScalarUnsafe(Unsafe.As<byte, uint>(ref Unsafe.AddByteOffset(ref source, i))).AsByte(),
                    Vector128.Create(value)).ExtractMostSignificantBits();

                if (mask != 0x0) goto ret_add_mask_tzc;

                length -= sizeof(uint);
                i += sizeof(uint);
            }
        }
        else
        {
            for (; length >= 4; i += 4)
            {
                length -= 4;
                if (Unsafe.AddByteOffset(ref source, i) == value) return (int)i;
                if (Unsafe.AddByteOffset(ref source, i + 1) == value) return (int)(i + 1);
                if (Unsafe.AddByteOffset(ref source, i + 2) == value) return (int)(i + 2);
                if (Unsafe.AddByteOffset(ref source, i + 3) == value) return (int)(i + 3);
            }
        }

        for (; length > 0; i++)
        {
            length--;
            if (Unsafe.AddByteOffset(ref source, i) == value) break;
        }

        return (int)i;
    ret_add_mask_tzc:
        return (int)(i + uint.TrailingZeroCount(mask));
    }
}