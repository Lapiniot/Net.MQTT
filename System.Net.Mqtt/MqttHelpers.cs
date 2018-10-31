using System.Buffers;
using System.Buffers.Binary;
using System.Net.Mqtt.Buffers;
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

        public static bool TryParseHeader(in ReadOnlySequence<byte> sequence, out byte flags, out int length, out int offset)
        {
            flags = 0;
            length = 0;
            offset = 0;

            if(sequence.IsEmpty) return false;

            if(sequence.IsSingleSegment)
            {
                return TryParseHeader(sequence.First.Span, out flags, out length, out offset);
            }

            var e = new SequenceEnumerator<byte>(sequence);

            if(!e.MoveNext()) return false;

            var first = e.Current;

            for(int i = 0, total = 0, m = 1; i < 4; i++, m <<= 7)
            {
                if(!e.MoveNext()) return false;

                var x = e.Current;

                total += (x & 0b01111111) * m;

                if((x & 0b10000000) != 0) continue;

                flags = first;
                length = total;
                offset = i + 2;
                return true;
            }

            return false;
        }

        public static bool TryParseHeader(in ReadOnlySpan<byte> buffer, out byte flags, out int length, out int offset)
        {
            length = 0;
            offset = 0;
            flags = 0;

            var threshold = Min(5, buffer.Length);

            for(int i = 1, total = 0, m = 1; i < threshold; i++, m <<= 7)
            {
                var x = buffer[i];

                total += (x & 0b01111111) * m;

                if((x & 0b10000000) != 0) continue;

                length = total;
                offset = i + 1;
                flags = buffer[0];
                return true;
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