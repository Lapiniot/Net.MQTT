using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Net.Mqtt
{
    public abstract class MqttMessage
    {
        public abstract Memory<byte> GetBytes();

        public virtual QoSLevel QoSLevel { get; set; }

        public virtual bool Duplicate { get; set; }

        public virtual bool Retain { get; set; }

        protected static int GetLengthByteCount(int length)
        {
            //return length <= 127 ? 1 : (length <= 16383 ? 2 : (length <= 2097151 ? 3 : 4));
            return (int)Math.Log(length, 0x80) + 1;
        }
        protected static int WriteString(Span<byte> destination, string str)
        {
            var count = UTF8.GetBytes(str.AsSpan(), destination.Slice(2));
            WriteUInt16BigEndian(destination, (ushort)count);
            return count + 2;
        }

        protected static int EncodeLengthBytes(int length, Span<byte> buffer)
        {
            var a = length / 128;
            var b = length % 128;

            if(a == 0)
            {
                buffer[0] = (byte)b;
                return 1;
            }
            else
            {
                buffer[0] = (byte)(b | 128);
                b = a % 128;
                a = a / 128;
                if(a == 0)
                {
                    buffer[1] = (byte)b;
                    return 2;
                }
                else
                {
                    buffer[1] = (byte)(b | 128);
                    b = a % 128;
                    a = a / 128;

                    if(a == 0)
                    {
                        buffer[2] = (byte)b;
                        return 3;
                    }
                    else
                    {
                        buffer[2] = (byte)(b | 128);
                        buffer[3] = (byte)a;
                        return 4;
                    }
                }
            }
        }
    }
}