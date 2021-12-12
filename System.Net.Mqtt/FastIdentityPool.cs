using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt;

// TODO: Check performance characteristics of SpinLock vs regular lock(){} for synchronization purposes
// TODO: Make BucketSize configurable
// TODO: Experiment with bucket sizes smaller than current minimum 32 bytes

/// <summary>
/// Implements fast concurrent id pool, which uses contiguous arrays and direct indexing to maintain state
/// </summary>
public class FastIdentityPool : IdentityPool
{
    private const int MinBucketSize = 32;
    private const int MaxBucketSize = 8192;
    private readonly short bucketSize;
    private readonly Bucket first;

    /// <summary>
    /// Creates instance of the type
    /// </summary>
    /// <param name="bucketSize">Bucket size</param>
    /// <remarks>
    /// Current implementation stores info about rented ids in the linked list of buckets
    /// (smaller arrays of <paramref name="bucketSize" /> fixed size) which grows on-demand.
    /// By default, only first bucket is allocated for performance reasons. Intensive calls
    /// to the <see cref="Rent" /> without subsequent calls to <see cref="Release" /> make list growing, allocating
    /// more memory. However, normally, list is not expanded if rented ids are returned to the pool shortly.
    /// Also keep in mind, <paramref name="bucketSize" /> should be the power of 2 for performance reasons
    /// (in order to avoid fractions calculations).
    /// </remarks>
    public FastIdentityPool(short bucketSize = MinBucketSize)
    {
        if(bucketSize < MinBucketSize || bucketSize > MaxBucketSize || (bucketSize & (bucketSize - 1)) != 0)
        {
            throw new ArgumentException(Format(InvariantCulture, MustBePositivePowerOfTwoInRange, MinBucketSize, MaxBucketSize), nameof(bucketSize));
        }

        this.bucketSize = bucketSize;
        first = new Bucket(bucketSize);
    }

    public override ushort Rent()
    {
        var bucket = first;
        var shift = 1; // used to skip over forbidden initial value 0
        var bitsSize = bucketSize << 3;

        for(var offset = 0; ; offset += bitsSize)
        {
            lock(bucket)
            {
                var pool = bucket.Storage;

                for(var byteIndex = 0; byteIndex < bucketSize; byteIndex++)
                {
                    var block = pool[byteIndex];
                    for(int bitIndex = shift; bitIndex < 8; bitIndex++)
                    {
                        var mask = 0x1 << bitIndex;
                        if((block & mask) == 0)
                        {
                            pool[byteIndex] = (byte)(block | mask);
                            return (ushort)(offset + (byteIndex << 3) + bitIndex);
                        }
                    }
                    shift = 0;
                }


                if(offset + bitsSize >= 0xFFFF)
                {
                    break;
                }

                if(bucket.Next == null)
                {
                    bucket.Next = new Bucket(bucketSize);
                }
            }

            bucket = bucket.Next;
        }

        throw new InvalidOperationException(RanOutOfIdentifiers);
    }

    public override void Release(ushort identity)
    {
        var bitsSize = bucketSize << 3;
        var bucketIndex = identity / bitsSize;
        var byteIndex = (identity % bitsSize) >> 3;
        var bitIndex = identity % bitsSize % 8;

        var bucket = first;

        for(var i = 0; bucket != null && i < bucketIndex; i++)
        {
            bucket = bucket.Next;

            if(bucket == null)
            {
                throw new InvalidOperationException(Format(InvariantCulture, IdIsNotTrackedByPoolFormat, identity));
            }
        }

        lock(bucket)
        {
            var block = bucket.Storage[byteIndex];
            var mask = 0x1 << bitIndex;
            if((block & mask) == 0)
            {
                throw new InvalidOperationException(Format(InvariantCulture, IdIsNotTrackedByPoolFormat, identity));
            }

            bucket.Storage[byteIndex] = (byte)(block & ~mask);
        }
    }

    private class Bucket
    {
        public readonly byte[] Storage;
        public Bucket Next;

        public Bucket(short size)
        {
            Storage = new byte[size];
        }
    }
}