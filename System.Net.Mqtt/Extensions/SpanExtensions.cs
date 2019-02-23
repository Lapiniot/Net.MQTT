using System.Buffers.Binary;
using System.Text;

namespace System.Net.Mqtt.Extensions
{
    public static class SpanExtensions
    {
        public static bool TryReadMqttHeader(this in ReadOnlySpan<byte> span, out byte header, out int size, out int offset)
        {
            size = 0;
            offset = 0;
            header = 0;

            var threshold = Math.Min(5, span.Length);

            for(int i = 1, total = 0, m = 1; i < threshold; i++, m <<= 7)
            {
                var x = span[i];

                total += (x & 0b01111111) * m;

                if((x & 0b10000000) != 0) continue;

                size = total;
                offset = i + 1;
                header = span[0];
                return true;
            }

            return false;
        }

        public static bool TryReadMqttString(this in ReadOnlySpan<byte> span, out string value, out int consumed)
        {
            value = null;
            consumed = 0;

            if(span.Length < 2) return false;

            var length = BinaryPrimitives.ReadUInt16BigEndian(span);

            if(length + 2 > span.Length) return false;

            value = Encoding.UTF8.GetString(span.Slice(2, length));
            consumed = 2 + length;

            return true;
        }

        public static int GetLengthByteCount(int length)
        {
            return length == 0 ? 1 : (int)Math.Log(length, 128) + 1;
        }

        public static int EncodeMqttString(ref Span<byte> span, string str)
        {
            var count = Encoding.UTF8.GetBytes(str.AsSpan(), span.Slice(2));
            BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)count);
            return count + 2;
        }

        public static int EncodeMqttLengthBytes(ref Span<byte> span, int length)
        {
            var v = length;
            var count = 0;

            do
            {
                var b = v % 128;
                v /= 128;
                span[count++] = (byte)(v > 0 ? b | 128 : b);
            } while(v > 0);

            return count;
        }
    }
}