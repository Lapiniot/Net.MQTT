﻿namespace Net.Mqtt.Extensions;

public static class SequenceReaderExtensions
{
    public static bool TryReadMqttVarByteInteger(ref SequenceReader<byte> reader, out int value)
    {
        value = 0;
        var consumed = reader.Consumed;
        for (int i = 0, m = 1; i < 4 && reader.TryRead(out var x); i++, m <<= 7)
        {
            value += (x & 0b01111111) * m;
            if ((x & 0b10000000) != 0) continue;
            return true;
        }

        reader.Rewind(reader.Consumed - consumed);
        value = 0;
        return false;
    }

    public static bool TryReadMqttString(ref SequenceReader<byte> reader, out byte[] value)
    {
        value = null;

        if (!reader.TryReadBigEndian(out short signed))
            return false;

        var length = (ushort)signed;

        if (length > reader.Remaining)
        {
            reader.Rewind(2);
            return false;
        }

        value = new byte[length];
        reader.TryCopyTo(value);

        reader.Advance(length);
        return true;
    }

    public static bool TryReadMqttHeader(ref SequenceReader<byte> reader,
        out byte controlHeader, out int remainingLength)
    {
        remainingLength = 0;

        var consumed = reader.Consumed;

        if (!reader.TryRead(out controlHeader)) return false;

        for (int i = 0, m = 1; i < 4 && reader.TryRead(out var x); i++, m <<= 7)
        {
            remainingLength += (x & 0b01111111) * m;
            if ((x & 0b10000000) != 0) continue;
            return true;
        }

        reader.Rewind(reader.Consumed - consumed);
        controlHeader = 0;
        remainingLength = 0;

        return false;
    }
}