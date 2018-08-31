using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using static System.Math;

namespace System.Net.Mqtt
{
    public static class MqttHelpers
    {
        public static int GetLengthByteCount(int length)
        {
            return length == 0 ? 1 : (int)Log(length, 128) + 1;
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

        public static bool TryParseHeader(in ReadOnlySequence<byte> sequence, out byte packetFlags, out int length)
        {
            packetFlags = 0;
            length = 0;

            if(sequence.IsEmpty) return false;

            packetFlags = sequence.First.Span[0];

            if(sequence.IsSingleSegment)
            {
                var span = sequence.First.Span.Slice(1);

                var len = Min(4, span.Length);

                for(int i = 0, mul = 1; i < len; i++, mul *= 128)
                {
                    var x = span[i];

                    length += (x & 0x7F) * mul;

                    if((x & 128) == 0) return true;
                }
            }
            else
            {
                var s = sequence.Slice(1);
                var mul = 1;
                foreach(var memory in s)
                {
                    var span = memory.Span;

                    var len = Min(4, span.Length);

                    for(var i = 0; i < len; i++, mul *= 128)
                    {
                        var x = span[i];

                        length += (x & 0x7F) * mul;

                        if((x & 128) == 0) return true;
                    }
                }
            }

            return false;
        }

        public static bool TryReadUInt16(ReadOnlySequence<byte> sequence, out ushort value)
        {
            value = 0;

            if(sequence.First.Length >= 2)
            {
                var span = sequence.First.Span;
                value = (ushort)((span[0] << 8) | span[1]);
                return true;
            }

            var consumed = 0;
            var v = 0;
            foreach(var m in sequence)
            {
                var span = m.Span;

                for(var i = 0; i < span.Length; i++)
                {
                    v |= span[i] << (8 * (1 - consumed));
                    if(++consumed == 2)
                    {
                        value = (ushort)v;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}