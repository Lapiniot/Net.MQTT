using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Net.Mqtt
{
    public abstract class MqttMessage
    {
        public virtual QoSLevel QoSLevel { get; set; }

        public virtual bool Duplicate { get; set; }

        public virtual bool Retain { get; set; }

        public abstract Memory<byte> GetBytes();

        protected static int GetLengthByteCount(int length)
        {
            return (int)Math.Log(length, 128) + 1;
        }

        protected static int EncodeString(string str, Span<byte> destination)
        {
            var count = UTF8.GetBytes(str.AsSpan(), destination.Slice(2));
            WriteUInt16BigEndian(destination, (ushort)count);
            return count + 2;
        }

        protected static int EncodeLengthBytes(int length, Span<byte> destination)
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