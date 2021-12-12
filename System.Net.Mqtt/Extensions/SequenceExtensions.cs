using System.Buffers;
using static System.Text.Encoding;

namespace System.Net.Mqtt.Extensions;

public static class SequenceExtensions
{
    public static bool TryReadUInt16(in ReadOnlySequence<byte> sequence, out ushort value)
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

    public static bool TryReadByte(in ReadOnlySequence<byte> sequence, out byte value)
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

    public static bool TryReadMqttString(in ReadOnlySequence<byte> sequence, out string value, out int consumed)
    {
        var span = sequence.FirstSpan;

        if(SpanExtensions.TryReadMqttString(in span, out value, out consumed))
        {
            return true;
        }

        if(!TryReadUInt16(sequence, out var length) || length + 2 > sequence.Length)
        {
            return false;
        }

        var sliced = sequence.Slice(2, length);
        // TODO: try to use stackallock byte[length] for small strings (how long?)
        value = sliced.IsSingleSegment ? UTF8.GetString(sliced.FirstSpan) : UTF8.GetString(sliced.ToArray());
        consumed = length + 2;
        return true;
    }

    public static bool TryReadMqttHeader(in ReadOnlySequence<byte> sequence, out byte header, out int length, out int offset)
    {
        var span = sequence.FirstSpan;

        if(SpanExtensions.TryReadMqttHeader(in span, out header, out length, out offset))
        {
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        if(SequenceReaderExtensions.TryReadMqttHeader(ref reader, out header, out length))
        {
            offset = (int)reader.Consumed;
            return true;
        }
        else
        {
            offset = 0;
            return false;
        }
    }
}