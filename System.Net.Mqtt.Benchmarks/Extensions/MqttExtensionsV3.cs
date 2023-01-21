using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Net.Mqtt.Benchmarks.Extensions;

public static class MqttExtensionsV3
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

        ref var f_ref = ref Unsafe.AsRef(in filter[0]);
        ref var t_ref = ref Unsafe.AsRef(in topic[0]);

        while (f_len > 0)
        {
            var c = f_ref;

            if (t_len > 0)
            {
                if (c != t_ref)
                {
                    if (c != '+') return c == '#';

                    while (t_len > 0 && t_ref != '/')
                    {
                        t_len--;
                        t_ref = ref Unsafe.Add(ref t_ref, 1);
                    }
                }
                else
                {
                    t_len--;
                    t_ref = ref Unsafe.Add(ref t_ref, 1);
                }

                f_len--;
                f_ref = ref Unsafe.Add(ref f_ref, 1);
            }
            else
            {
                return c == '#' || c == '+' || c == '/' && f_len > 1 && Unsafe.Add(ref f_ref, 1) == '#';
            }
        }

        return t_len == 0;
    }
}