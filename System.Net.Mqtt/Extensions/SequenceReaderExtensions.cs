using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.Mqtt.Extensions
{
    public static class SequenceReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ushort value)
        {
            if(reader.TryReadBigEndian(out short v))
            {
                value = (ushort)v;
                return true;
            }

            value = 0;
            return false;
        }

        public static bool TryReadMqttString(this ref SequenceReader<byte> reader, out string value)
        {
            value = null;

            // Very hot path: single buffer sequence
            if(reader.Sequence.IsSingleSegment)
            {
                if(!MqttHelpers.TryReadString(reader.UnreadSpan, out value, out var consumed)) return false;
                reader.Advance(consumed);
                return true;
            }

            if(!reader.TryReadBigEndian(out var length)) return false;

            if(length > reader.Remaining)
            {
                reader.Rewind(2);
                return false;
            }

            var span = reader.UnreadSpan;
            if(span.Length >= length)
            {
                // Hot path: string data is in the solid buffer
                value = Encoding.UTF8.GetString(span.Slice(0, length));
                reader.Advance(length);
                return true;
            }

            // Worst case: segmented sequence, need to copy into temporary buffer
            Span<byte> buffer = new byte[length];
            if(!reader.TryCopyTo(buffer)) return false;
            value = Encoding.UTF8.GetString(buffer);
            reader.Advance(length);
            return true;
        }

        public static bool TryReadMqttHeader(this ref SequenceReader<byte> reader, out byte header, out int length)
        {
            header = 0;
            length = 0;

            if(reader.Length < 2) return false;

            var remaining = reader.Remaining;

            // Fast path
            if(reader.CurrentSpan.Length >= 5)
            {
                if(!MqttHelpers.TryParseHeader(reader.UnreadSpan, out header, out length, out var offset)) return false;
                reader.Advance(offset);
                return true;
            }

            reader.TryRead(out header);

            for(int i = 0, m = 1; i < 4; i++, m <<= 7)
            {
                if(!reader.TryRead(out var x)) break;

                length += (x & 0b01111111) * m;

                if((x & 0b10000000) != 0) continue;

                return true;
            }

            reader.Rewind(remaining - reader.Remaining);
            header = 0;
            length = 0;
            return false;
        }
    }
}