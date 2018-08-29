using System.Buffers.Binary;
using System.Text;

namespace System.Net.Mqtt
{
    public static class MqttHelpers
    {
        public static int GetLengthByteCount(int length)
        {
            return length == 0 ? 1 : (int)Math.Log(length, 128) + 1;
        }

        public static int EncodeString(string str, Span<byte> destination)
        {
            var count = Encoding.UTF8.GetBytes(str.AsSpan(), destination.Slice(2));
            BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)count);
            return count + 2;
        }

        public static int EncodeLengthBytes(int length, Span<byte> destination)
        {
            var v = length;
            var count = 0;

            do
            {
                var b = v % 128;
                v = v / 128;
                destination[count++] = (byte)(v > 0 ? b | 128 : b);
            } while(v > 0);

            return count;
        }
    }
}