namespace System.Net.Mqtt.Tests;

public static class SequenceFactory
{
    public static ReadOnlySequence<T> Create<T>(T[] segment1, T[] segment2)
    {
        ArgumentNullException.ThrowIfNull(segment1);
        ArgumentNullException.ThrowIfNull(segment2);

        var segment = new MemorySegment<T>(segment1);
        return new ReadOnlySequence<T>(segment, 0, segment.Append(segment2), segment2.Length);
    }

    public static ReadOnlySequence<T> Create<T>(T[] segment1, T[] segment2, T[] segment3)
    {
        ArgumentNullException.ThrowIfNull(segment1);
        ArgumentNullException.ThrowIfNull(segment2);
        ArgumentNullException.ThrowIfNull(segment3);

        var segment = new MemorySegment<T>(segment1);
        return new ReadOnlySequence<T>(segment, 0, segment.Append(segment2).Append(segment3), segment3.Length);
    }

    public static ReadOnlySequence<T> Create<T>(T[] segment1, T[] segment2, T[] segment3, T[] segment4)
    {
        ArgumentNullException.ThrowIfNull(segment1);
        ArgumentNullException.ThrowIfNull(segment2);
        ArgumentNullException.ThrowIfNull(segment3);
        ArgumentNullException.ThrowIfNull(segment4);

        var segment = new MemorySegment<T>(segment1);
        return new ReadOnlySequence<T>(segment, 0, segment.Append(segment2).Append(segment3).Append(segment4), segment4.Length);
    }

    public static ReadOnlySequence<T> Create<T>(T[] segment1, T[] segment2, T[] segment3, T[] segment4, ReadOnlyMemory<T> segment5)
    {
        ArgumentNullException.ThrowIfNull(segment1);
        ArgumentNullException.ThrowIfNull(segment2);
        ArgumentNullException.ThrowIfNull(segment3);
        ArgumentNullException.ThrowIfNull(segment4);
        ArgumentNullException.ThrowIfNull(segment5);

        var segment = new MemorySegment<T>(segment1);
        return new ReadOnlySequence<T>(segment, 0, segment.Append(segment2).Append(segment3).Append(segment4).Append(segment5), segment5.Length);
    }
}