using System.Buffers;

namespace System.Net.Mqtt.Tests
{
    internal class Segment<T> : ReadOnlySequenceSegment<T>
    {
        public Segment(T[] array)
        {
            Memory = array;
        }

        public Segment<T> Append(T[] array)
        {
            var segment = new Segment<T>(array) {RunningIndex = RunningIndex + Memory.Length};
            Next = segment;
            return segment;
        }
    }
}