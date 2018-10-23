using System.Buffers;

namespace System.Net.Mqtt.Buffers
{
    public ref struct SequenceEnumerator<T>
    {
        private ReadOnlySequence<T> sequence;
        private SequencePosition sequencePosition;
        private int index;
        private ReadOnlySpan<T> span;

        public SequenceEnumerator(ReadOnlySequence<T> sequence)
        {
            this.sequence = sequence;
            sequencePosition = sequence.Start;
            index = 0;
            span = default;
        }

        public bool MoveNext()
        {
            if(index == -1) return false;

            if(index < span.Length - 1)
            {
                index++;
                return true;
            }

            if(sequence.TryGet(ref sequencePosition, out var memory))
            {
                span = memory.Span;
                index = 0;
                return true;
            }

            index = -1;
            return false;
        }

        public void Reset()
        {
            span = default;
            index = 0;
        }

        public ref readonly T Current => ref span[index];
    }
}