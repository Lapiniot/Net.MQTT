using System.Buffers;
using System.Text;

namespace System.Net.Mqtt.Extensions;

public static class SequenceReaderExtensions
{
    public static bool TryReadMqttString(ref this SequenceReader<byte> reader, out string value)
    {
        value = null;

        // Very hot path: single buffer sequence
        if(reader.Sequence.IsSingleSegment)
        {
            if(!reader.UnreadSpan.TryReadMqttString(out value, out var consumed)) return false;
            reader.Advance(consumed);
            return true;
        }

        if(!reader.TryReadBigEndian(out short signed)) return false;

        var length = (ushort)signed;

        if(length > reader.Remaining)
        {
            reader.Rewind(2);
            return false;
        }

        var span = reader.UnreadSpan;
        if(span.Length >= length)
        {
            // Hot path: string data is in the solid buffer
            value = Encoding.UTF8.GetString(span[..length]);
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

    public static bool TryReadMqttHeader(ref this SequenceReader<byte> reader, out byte header, out int length)
    {
        length = 0;

        var consumed = reader.Consumed;

        if(!reader.TryRead(out header)) return false;

        for(int i = 0, m = 1; i < 4 && reader.TryRead(out var x); i++, m <<= 7)
        {
            length += (x & 0b01111111) * m;
            if((x & 0b10000000) != 0) continue;
            return true;
        }

        reader.Rewind(reader.Consumed - consumed);
        header = 0;
        length = 0;

        return false;
    }
}