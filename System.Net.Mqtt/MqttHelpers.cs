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
            var a = length / 128;
            var b = length % 128;

            if(a == 0)
            {
                destination[0] = (byte)b;
                return 1;
            }

            destination[0] = (byte)(b | 128);
            b = a % 128;
            a = a / 128;
            if(a == 0)
            {
                destination[1] = (byte)b;
                return 2;
            }

            destination[1] = (byte)(b | 128);
            b = a % 128;
            a = a / 128;

            if(a == 0)
            {
                destination[2] = (byte)b;
                return 3;
            }

            destination[2] = (byte)(b | 128);
            destination[3] = (byte)a;
            return 4;
        }
    }
}