using System.Globalization;

namespace System.Net.Mqtt.Extensions;

public static class SpanExtensions
{
    public static bool TryReadMqttVarByteInteger(ReadOnlySpan<byte> span, out int value, out int consumed)
    {
        value = 0;
        var threshold = Math.Min(4, span.Length);
        for (int i = 0, m = 1; i < threshold; i++, m <<= 7)
        {
            var x = span[i];
            value += (x & 0b01111111) * m;
            if ((x & 0b10000000) != 0) continue;
            consumed = i + 1;
            return true;
        }

        consumed = 0;
        return false;
    }

    public static bool TryReadMqttHeader(ReadOnlySpan<byte> span, out byte header, out int length, out int offset)
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

    public static bool TryReadMqttString(ReadOnlySpan<byte> span, out byte[] value, out int consumed)
    {
        value = null;
        consumed = 0;

        if (span.Length < 2) return false;

        var length = BinaryPrimitives.ReadUInt16BigEndian(span);

        if (length + 2 > span.Length) return false;

        value = span.Slice(2, length).ToArray();
        consumed = 2 + length;

        return true;
    }

    public static void WriteMqttString(ref Span<byte> span, ReadOnlySpan<byte> value)
    {
        value.CopyTo(span.Slice(2));
        var length = value.Length;
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)length);
        span = span.Slice(length + 2);
    }

    public static void WriteMqttUserProperty(ref Span<byte> span, ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        span[0] = 0x26;
        span = span.Slice(1);
        WriteMqttString(ref span, key);
        WriteMqttString(ref span, value);
    }

    public static void WriteMqttVarByteInteger(ref Span<byte> span, int value)
    {
        var v = value;
        var count = 0;

        do
        {
            var b = v & 0x7F;
            v >>>= 7;
            span[count++] = (byte)(v > 0 ? b | 0x80 : b);
        } while (v > 0);

        span = span.Slice(count);
    }

    [Conditional("DEBUG")]
    public static void DebugDump(ReadOnlySpan<byte> span) =>
        Debug.WriteLine($"{{{string.Join(",", span.ToArray().Select(b => "0x" + b.ToString("x2", CultureInfo.InvariantCulture)))}}}");
}