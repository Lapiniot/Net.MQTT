using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Net.Mqtt.Benchmarks.Extensions;

public static class MqttExtensionsV4
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
            var offset = CommonPrefixLength(ref t_ref, ref f_ref, Math.Min(t_len, f_len));

            if (offset > 0)
            {
                if (offset == t_len && offset == f_len) return true;

                t_len -= offset;
                f_len -= offset;
                t_ref = ref Unsafe.Add(ref t_ref, offset);
                f_ref = ref Unsafe.Add(ref f_ref, offset);
            }

            if (f_len == 0) return t_len == 0;

            var c = f_ref;

            if (c == '+')
            {
                f_ref = ref Unsafe.Add(ref f_ref, 1);
                f_len--;

                offset = SegmentLength(ref t_ref, t_len);

                t_ref = ref Unsafe.Add(ref t_ref, offset);
                t_len -= offset;
            }
            else
            {
                return c == '#' || c == '/' && t_len == 0 && f_len > 1 && Unsafe.Add(ref f_ref, 1) == '#';
            }
        } while (f_len > 0 || t_len > 0);

        return true;
    }

    [MethodImpl(AggressiveInlining)]
    private static int CommonPrefixLength(ref byte left, ref byte right, int length)
    {
        nuint index = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            //hardware SIMD instructions are supported and data is large enough:
            //try to process as many 16-byte vectors as it fits in the given length first of all

            if (length >= Vector128<byte>.Count)
            {
                while (length >= Vector128<byte>.Count)
                {
                    length -= Vector128<byte>.Count;

                    var mask = Vector128.Equals(
                        Vector128.LoadUnsafe(ref left, index),
                        Vector128.LoadUnsafe(ref right, index))
                            .ExtractMostSignificantBits();

                    if (mask != 0xFFFF)
                        return (int)(index + uint.TrailingZeroCount(~mask));

                    index += (nuint)Vector128<byte>.Count;
                }
            }
        }
        else
        {
            // Else use loop unrolling with 8 tests per iteration
            while (length >= 8)
            {
                length -= 8;
                if (Unsafe.Add(ref left, index) != Unsafe.Add(ref right, index)) goto ret;
                if (Unsafe.Add(ref left, index + 1) != Unsafe.Add(ref right, index + 1)) goto ret_add_1;
                if (Unsafe.Add(ref left, index + 2) != Unsafe.Add(ref right, index + 2)) goto ret_add_2;
                if (Unsafe.Add(ref left, index + 3) != Unsafe.Add(ref right, index + 3)) goto ret_add_3;
                if (Unsafe.Add(ref left, index + 4) != Unsafe.Add(ref right, index + 4)) goto ret_add_4;
                if (Unsafe.Add(ref left, index + 5) != Unsafe.Add(ref right, index + 5)) goto ret_add_5;
                if (Unsafe.Add(ref left, index + 6) != Unsafe.Add(ref right, index + 6)) goto ret_add_6;
                if (Unsafe.Add(ref left, index + 7) != Unsafe.Add(ref right, index + 7)) goto ret_add_7;
                index += 8;
            }
        }

        while (length >= 4)
        {
            length -= 4;
            if (Unsafe.Add(ref left, index) != Unsafe.Add(ref right, index)) goto ret;
            if (Unsafe.Add(ref left, index + 1) != Unsafe.Add(ref right, index + 1)) goto ret_add_1;
            if (Unsafe.Add(ref left, index + 2) != Unsafe.Add(ref right, index + 2)) goto ret_add_2;
            if (Unsafe.Add(ref left, index + 3) != Unsafe.Add(ref right, index + 3)) goto ret_add_3;
            index += 4;
        }

        while (length > 0)
        {
            length--;
            if (Unsafe.Add(ref left, index) != Unsafe.Add(ref right, index)) goto ret;
            index++;
        }

    ret:
        return (int)index;
    ret_add_1:
        return (int)(index + 1);
    ret_add_2:
        return (int)(index + 2);
    ret_add_3:
        return (int)(index + 3);
    ret_add_4:
        return (int)(index + 4);
    ret_add_5:
        return (int)(index + 5);
    ret_add_6:
        return (int)(index + 6);
    ret_add_7:
        return (int)(index + 7);
    }

    [MethodImpl(AggressiveInlining)]
    private static int SegmentLength(ref byte source, int length)
    {
        nuint index = 0;
        const byte value = 0x2f;

        if (Vector128.IsHardwareAccelerated)
        {
            if (length >= Vector128<byte>.Count)
            {

                while (length >= Vector128<byte>.Count)
                {
                    var mask = Vector128.Equals(
                        Vector128.LoadUnsafe(ref source, index),
                        Vector128.Create(value))
                            .ExtractMostSignificantBits();

                    if (mask != 0x0000)
                        return (int)(index + uint.TrailingZeroCount(mask));

                    length -= Vector128<byte>.Count;
                    index += (nuint)Vector128<byte>.Count;
                }
            }
        }
        else
        {
            while (length >= 8)
            {
                length -= 8;
                if (Unsafe.Add(ref source, index) == value) goto ret;
                if (Unsafe.Add(ref source, index + 1) == value) goto ret_add_1;
                if (Unsafe.Add(ref source, index + 2) == value) goto ret_add_2;
                if (Unsafe.Add(ref source, index + 3) == value) goto ret_add_3;
                if (Unsafe.Add(ref source, index + 4) == value) goto ret_add_4;
                if (Unsafe.Add(ref source, index + 5) == value) goto ret_add_5;
                if (Unsafe.Add(ref source, index + 6) == value) goto ret_add_6;
                if (Unsafe.Add(ref source, index + 7) == value) goto ret_add_7;
                index += 8;
            }
        }

        while (length >= 4)
        {
            length -= 4;
            if (Unsafe.Add(ref source, index) == value) goto ret;
            if (Unsafe.Add(ref source, index + 1) == value) goto ret_add_1;
            if (Unsafe.Add(ref source, index + 2) == value) goto ret_add_2;
            if (Unsafe.Add(ref source, index + 3) == value) goto ret_add_3;
            index += 4;
        }

        while (length > 0)
        {
            length--;
            if (Unsafe.Add(ref source, index) == value) break;
            index += 1;
        }

    ret:
        return (int)index;
    ret_add_1:
        return (int)(index + 1);
    ret_add_2:
        return (int)(index + 2);
    ret_add_3:
        return (int)(index + 3);
    ret_add_4:
        return (int)(index + 4);
    ret_add_5:
        return (int)(index + 5);
    ret_add_6:
        return (int)(index + 6);
    ret_add_7:
        return (int)(index + 7);
    }
}