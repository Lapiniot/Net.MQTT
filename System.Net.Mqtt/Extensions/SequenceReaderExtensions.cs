using System.Buffers;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Extensions;

public static class SequenceReaderExtensions
{
    private const int MaxStackAllocSize = 512;

    public static bool TryReadMqttString(ref SequenceReader<byte> reader, out string value)
    {
        value = null;

        if(!reader.TryReadBigEndian(out short signed))
        {
            return false;
        }

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
            value = UTF8.GetString(span[..length]);
        }
        else
        {
            // Worst case: segmented sequence, need to copy into temporary buffer
            if(length <= MaxStackAllocSize)
            {
                Span<byte> bytes = stackalloc byte[length];
                reader.TryCopyTo(bytes);
                value = UTF8.GetString(bytes);
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                try
                {
                    var bytes = buffer.AsSpan(0, length);
                    reader.TryCopyTo(bytes);
                    value = UTF8.GetString(bytes);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        reader.Advance(length);
        return true;
    }

    public static bool TryReadMqttHeader(ref SequenceReader<byte> reader, out byte header, out int length)
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