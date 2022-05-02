﻿namespace System.Net.Mqtt.Extensions;

public static class SpanExtensions
{
    public static bool TryReadMqttHeader(in ReadOnlySpan<byte> span, out byte header, out int length, out int offset)
    {
        length = 0;
        offset = 0;
        header = 0;

        var threshold = Math.Min(5, span.Length);

        for (int i = 1, m = 1; i < threshold; i++, m <<= 7)
        {
            var x = span[i];

            length += (x & 0b01111111) * m;

            if ((x & 0b10000000) != 0) continue;

            offset = i + 1;
            header = span[0];
            return true;
        }

        return false;
    }

    public static bool TryReadMqttString(in ReadOnlySpan<byte> span, out byte[] value, out int consumed)
    {
        value = null;
        consumed = 0;

        if (span.Length < 2) return false;

        var length = BP.ReadUInt16BigEndian(span);

        if (length + 2 > span.Length) return false;

        value = span.Slice(2, length).ToArray();
        consumed = 2 + length;

        return true;
    }

    public static int WriteMqttString(ref Span<byte> span, ReadOnlySpan<byte> utf8Str)
    {
        utf8Str.CopyTo(span[2..]);
        var length = utf8Str.Length;
        BP.WriteUInt16BigEndian(span, (ushort)length);
        return length + 2;
    }

    public static int WriteMqttLengthBytes(ref Span<byte> span, int length)
    {
        var v = length;
        var count = 0;

        do
        {
            var b = v & 0x7F;
            v >>= 7;
            span[count++] = (byte)(v > 0 ? b | 0x80 : b);
        } while (v > 0);

        return count;
    }
}