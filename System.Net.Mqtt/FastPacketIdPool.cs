using System.Threading;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Implements fast concurrent non-blocking id pool, which uses contiguous array
    /// and direct indexing to maintain state
    /// <remarks>
    /// Fast non-blocking synchronization is provided at a cost of bigger memory consumption,
    /// as soon as 65536*4 bytes of state data per instance is uses to store state
    /// </remarks>
    /// </summary>
    public class FastPacketIdPool : IPacketIdPool
    {
        private ushort bucketSize;
        private readonly Bucket first;

        public FastPacketIdPool(ushort bucketSize = 32)
        {
            if(bucketSize <= 0 || (bucketSize & (bucketSize - 1)) != 0)
                throw new ArgumentException("Must be positive number which is power of two", nameof(bucketSize));
            this.bucketSize = bucketSize;
            first = new Bucket(bucketSize);
        }

        public ushort Rent()
        {
            var current = first;
            for(int offset = 0; ; offset += bucketSize)
            {
                for(int i = 0; i < bucketSize; i++)
                {
                    if(Interlocked.CompareExchange(ref current.pool[i], 1, 0) == 0) return checked ((ushort)(offset + i + 1));
                }

                if(offset + bucketSize >= 0xFFFF) break;

                var c = Volatile.Read(ref current.next);
                if(c != null) { current = c; continue; }

                ref var next = ref current.next;

                if(Interlocked.CompareExchange(ref current.locked, 1, 0) == 0)
                {
                    Volatile.Write(ref next, current = new Bucket(bucketSize));
                }
                else
                {
                    SpinWait spinner = new SpinWait();
                    while((current = Volatile.Read(ref next)) == null) spinner.SpinOnce();
                }
            };

            throw new InvalidOperationException(RanOutOfIdentifiers);
        }

        public void Return(ushort id)
        {
            var bucketIndex = --id / bucketSize;
            var index = id % bucketSize;
            var current = first;
            for(int i = 0; i < bucketIndex; i++) current = current.next;
            Interlocked.Exchange(ref current.pool[index], 0);
        }

        private class Bucket
        {
            public int[] pool;
            public Bucket next;
            public int locked;
            public Bucket(ushort size)
            {
                pool = new int[size];
            }
        }
    }
}