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

        public static bool TryParseHeader(in ReadOnlySequence<byte> sequence, out byte packetFlags, out int length, out int dataOffset)
        {
            packetFlags = 0;
            length = 0;
            dataOffset = 0;

            if(sequence.IsEmpty) return false;

            if(sequence.IsSingleSegment)
            {
                return TryParseHeader(sequence.First.Span, out packetFlags, out length, out dataOffset);
            }

            packetFlags = sequence.First.Span[0];

            var s = sequence.Slice(1);
            var mul = 1;
            dataOffset = 1;
            foreach(var memory in s)
            {
                var span = memory.Span;

                var len = Min(4, span.Length);

                for(var i = 0; i < len; i++, mul *= 128)
                {
                    var x = span[i];

                    length += (x & 0x7F) * mul;

                    dataOffset++;

                    if((x & 128) == 0) return true;
                }
            }

            length = 0;
            dataOffset = 0;
            return false;
        }

        public static bool TryParseHeader(in ReadOnlySpan<byte> buffer, out byte packetFlags, out int length, out int dataOffset)
        {
            packetFlags = 0;
            length = 0;
            dataOffset = 0;

            if(buffer.Length == 0) return false;

            packetFlags = buffer[0];

            var len = Min(5, buffer.Length);

            for(int i = 1, mul = 1; i < len; i++, mul *= 128)
            {
                var x = buffer[i];

                length += (x & 0x7F) * mul;

                if((x & 128) == 0)
                {
                    dataOffset = i + 1;
                    return true;
                }
            }

            length = 0;
            dataOffset = 0;
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

        public static bool TryReadByte(in ReadOnlySequence<byte> sequence, out byte value)
        {
            if(sequence.First.Length > 0)
            {
                value = sequence.First.Span[0];
                return true;
            }

            foreach(var memory in sequence)
            {
                if(memory.Length > 0)
                {
                    value = memory.Span[0];
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public static bool TryReadString(in ReadOnlySpan<byte> source, out string value, out int consumed)
        {
            if(source.Length < 2)
            {
                value = null;
                consumed = 0;
                return false;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(source);

            if(length + 2 <= source.Length)
            {
                value = Encoding.UTF8.GetString(source.Slice(2, length));
                consumed = 2 + length;
                return true;
            }

            value = null;
            consumed = 0;
            return false;
        }

        public static bool TryReadString(ReadOnlySequence<byte> sequence, out string value, out int consumed)
        {
            value = null;
            consumed = 0;
            if(!TryReadUInt16(sequence, out var length) || length + 2 > sequence.Length) return false;

            value = sequence.First.Length >= length + 2
                ? Encoding.UTF8.GetString(sequence.First.Span.Slice(2, length))
                : Encoding.UTF8.GetString(sequence.Slice(2, length).ToArray());

            consumed = length + 2;

            return true;
        }
    }
}