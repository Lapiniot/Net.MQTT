using System.Runtime.CompilerServices;
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
        var t_length = topic.Length;
        var f_length = filter.Length;
        var t_index = 0;
        var f_index = 0;

        if (t_length == 0 || f_length == 0) return false;

        ref var t_ref = ref Unsafe.AsRef(in topic[0]);
        ref var f_ref = ref Unsafe.AsRef(in filter[0]);

        for (; f_index < f_length; f_index++)
        {
            var ch = Unsafe.AddByteOffset(ref f_ref, f_index);

            if (t_index < t_length)
            {
                if (ch != Unsafe.AddByteOffset(ref t_ref, t_index))
                {
                    if (ch != '+') return ch == '#';
                    // Scan and skip topic characters until level separator occurrence
                    while (t_index < t_length && Unsafe.AddByteOffset(ref t_ref, t_index) != '/') t_index++;
                    continue;
                }

                t_index++;
            }
            else
            {
                // Edge case: we ran out of characters in the topic sequence.
                // Return true only for proper topic filter level wildcard combination.
                return ch == '#'
                    || ch == '/' && f_index < f_length - 1 && Unsafe.AddByteOffset(ref f_ref, f_index + 1) == '#'
                    || ch == '+' && t_length > 0 && Unsafe.AddByteOffset(ref t_ref, t_length - 1) == '/';
            }
        }

        // return true only if topic character sequence has been completely scanned
        return t_index == t_length;
    }
}