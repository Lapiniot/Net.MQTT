using System.Buffers;
using System.Text;

namespace System.Net.Mqtt.Extensions;

public static class SequenceExtensions
{
    public static bool TryReadUInt16(this in ReadOnlySequence<byte> sequence, out ushort value)
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
                if(++consumed != 2) continue;
                value = (ushort)v;
                return true;
            }
        }

        return false;
    }

    public static bool TryReadByte(this in ReadOnlySequence<byte> sequence, out byte value)
    {
        if(sequence.First.Length > 0)
        {
            value = sequence.First.Span[0];
            return true;
        }

        foreach(var memory in sequence)
        {
            if(memory.Length == 0) continue;
            value = memory.Span[0];
            return true;
        }

        value = 0;
        return false;
    }

    public static bool TryReadMqttString(this ReadOnlySequence<byte> sequence, out string value, out int consumed)
    {
        value = null;
        consumed = 0;

        if(sequence.IsSingleSegment) return sequence.First.Span.TryReadMqttString(out value, out consumed);

        if(!TryReadUInt16(sequence, out var length) || length + 2 > sequence.Length) return false;

        sequence = sequence.Slice(2, length);

        value = sequence.IsSingleSegment
            ? Encoding.UTF8.GetString(sequence.First.Span)
            : Encoding.UTF8.GetString(sequence.ToArray());

        consumed = length + 2;

        return true;
    }

    public static bool TryReadMqttHeader(this in ReadOnlySequence<byte> sequence, out byte header, out int length, out int offset)
    {
        header = 0;
        length = 0;
        offset = 0;

        if(sequence.IsEmpty) return false;

        // Fast path
        if(sequence.IsSingleSegment || sequence.First.Length >= 5)
        {
            return sequence.First.Span.TryReadMqttHeader(out header, out length, out offset);
        }


        var sr = new SequenceReader<byte>(sequence);

        if(!sr.TryRead(out var first)) return false;

        for(int i = 0, total = 0, m = 1; i < 4; i++, m <<= 7)
        {
            if(!sr.TryRead(out var x)) return false;

            total += (x & 0b01111111) * m;

            if((x & 0b10000000) != 0) continue;

            header = first;
            length = total;
            offset = i + 2;
            return true;
        }

        return false;
    }
}