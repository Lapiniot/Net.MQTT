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
        var tlen = topic.Length;
        var flen = filter.Length;

        if (tlen == 0 || flen == 0) return false;

        var ti = 0;

        for (var fi = 0; fi < flen; fi++)
        {
            var ch = filter[fi];

            if (ti < tlen)
            {
                if (ch != topic[ti])
                {
                    if (ch != '+') return ch == '#';
                    while (ti < tlen && topic[ti] != '/') ti++;
                }
                else
                {
                    ti++;
                }
            }
            else
            {
                return ch == '#'
                    || ch == '/' && ++fi < flen && filter[fi] == '#'
                    || ch == '+' && tlen > 0 && topic[tlen - 1] == '/';
            }
        }

        return ti == tlen;
    }
}