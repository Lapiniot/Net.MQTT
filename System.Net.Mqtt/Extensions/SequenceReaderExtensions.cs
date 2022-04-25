namespace System.Net.Mqtt.Extensions;

public static class SequenceReaderExtensions
{
    public static bool TryReadMqttString(ref SequenceReader<byte> reader, out ReadOnlyMemory<byte> value)
    {
        value = null;

        if (!reader.TryReadBigEndian(out short signed))
        {
            return false;
        }

        var length = (ushort)signed;

        if (length > reader.Remaining)
        {
            reader.Rewind(2);
            return false;
        }

        var memory = new byte[length];
        reader.TryCopyTo(memory);
        value = memory;

        reader.Advance(length);
        return true;
    }

    public static bool TryReadMqttHeader(ref SequenceReader<byte> reader, out byte header, out int length)
    {
        length = 0;

        var consumed = reader.Consumed;

        if (!reader.TryRead(out header)) return false;

        for (int i = 0, m = 1; i < 4 && reader.TryRead(out var x); i++, m <<= 7)
        {
            length += (x & 0b01111111) * m;
            if ((x & 0b10000000) != 0) continue;
            return true;
        }

        reader.Rewind(reader.Consumed - consumed);
        header = 0;
        length = 0;

        return false;
    }
}