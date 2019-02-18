using System.Threading;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;
using static System.Threading.Interlocked;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Implements fast concurrent non-blocking id pool, which uses contiguous arrays
    /// and direct indexing to maintain state
    /// <remarks>
    /// Fast non-blocking synchronization is provided at a cost of bigger memory consumption,
    /// as soon as up to ~ 65536*4 bytes of state data per instance is used to store state
    /// </remarks>
    /// </summary>
    public class FastPacketIdPool : IPacketIdPool
    {
        private const int DefaultBucketSize = 32;
        private readonly short bucketSize;
        private readonly Bucket first;

        /// <summary>
        /// Creates instance of the type
        /// </summary>
        /// <param name="bucketSize">Bucket size</param>
        /// <remarks>
        /// Current implementation stores info about rented ids in the linked list of buckets
        /// (smaller arrays of <paramref name="bucketSize" /> fixed size) which grows on-demand.
        /// By default, only first bucket is allocated due to the performance reasons. Intensive calls
        /// to the <see cref="Rent" /> without subsequent calls to <see cref="Return" /> make list growing, allocating
        /// more memory. However, normally, list is not expanded if rented ids are returned to the pool shortly.
        /// Also keep in mind, <paramref name="bucketSize" /> should be the power of 2 for performance reasons
        /// (in order to avoid fractions calculations).
        /// </remarks>
        public FastPacketIdPool(short bucketSize = DefaultBucketSize)
        {
            if(bucketSize <= 0 || (bucketSize & (bucketSize - 1)) != 0)
            {
                throw new ArgumentException(MustBePositivePowerOfTwo, nameof(bucketSize));
            }

            if(bucketSize < DefaultBucketSize)
            {
                throw new ArgumentException(Format(MustNotBeLessThanMinimalFormat, DefaultBucketSize), nameof(bucketSize));
            }

            this.bucketSize = bucketSize;
            first = new Bucket(bucketSize);
        }

        public ushort Rent()
        {
            var bucket = first;
            var start = 1;

            for(var offset = 0;; offset += bucketSize)
            {
                for(var i = start; i < bucketSize; i++)
                {
                    if(CompareExchange(ref bucket.Pool[i], 1, 0) == 0) return (ushort)(offset + i);
                }

                start = 0;

                if(offset + bucketSize >= 0xFFFF) break;

                var n = Volatile.Read(ref bucket.Next);
                if(n != null)
                {
                    bucket = n;
                    continue;
                }

                ref var next = ref bucket.Next;
                if(CompareExchange(ref bucket.Closed, 1, 0) == 0)
                {
                    Volatile.Write(ref next, bucket = new Bucket(bucketSize));
                }
                else
                {
                    while((bucket = Volatile.Read(ref next)) == null) Thread.Yield();
                }
            }

            throw new InvalidOperationException(RanOutOfIdentifiers);
        }

        public void Return(ushort id)
        {
            var bucketIndex = id / bucketSize;
            var index = id % bucketSize;
            var bucket = first;

            for(var i = 0; bucket != null && i < bucketIndex; i++) bucket = bucket.Next;

            if(bucket == null || Exchange(ref bucket.Pool[index], 0) == 0)
            {
                throw new InvalidOperationException(Format(IdIsNotTrackedByPoolFormat, id));
            }
        }

        private class Bucket
        {
            public readonly int[] Pool;
            public int Closed;
            public Bucket Next;

            public Bucket(short size)
            {
                Pool = new int[size];
            }
        }
    }
}