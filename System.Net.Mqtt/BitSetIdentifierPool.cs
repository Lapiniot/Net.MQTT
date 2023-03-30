namespace System.Net.Mqtt;

#nullable enable

/// <summary>
/// Implements fast concurrent id pool, which uses contiguous arrays and direct indexing to maintain state
/// </summary>
public class BitSetIdentifierPool : IdentifierPool
{
    private const ushort MinBucketSize = 16;
    private const ushort MaxBucketSize = 8192;
    private const ushort DefaultBucketSize = 32;
    private readonly int bucketSize;
    private readonly Bucket first;

    /// <summary>
    /// Creates instance of the type
    /// </summary>
    /// <param name="bucketSize">Bucket size</param>
    /// <remarks>
    /// Current implementation stores info about rented ids in the linked list of buckets
    /// (smaller arrays of <paramref name="bucketSize" /> fixed size) which grows on-demand.
    /// By default, only first bucket is allocated for performance reasons. Intensive calls
    /// to the <see cref="Rent" /> without subsequent calls to <see cref="Return" /> make list growing, allocating
    /// more memory. However, normally, list is not expanded if rented ids are returned to the pool shortly.
    /// Also keep in mind, <paramref name="bucketSize" /> should be the power of 2 for performance reasons
    /// (in order to avoid fractions calculations).
    /// </remarks>
    public BitSetIdentifierPool(int bucketSize = DefaultBucketSize)
    {
        Verify.ThrowIfNotInRange(bucketSize, MinBucketSize, MaxBucketSize);
        Verify.ThrowIfNotPowerOfTwo(bucketSize);

        this.bucketSize = bucketSize;
        first = new((int)((uint)bucketSize / (uint)nuint.Size));
        // 0 - is invalid packet id according to MQTT spec, so reserve it right away
        first.Storage[0] = 0x1;
    }

    public override ushort Rent()
    {
        var bucket = first;

        // bits count per bucket
        var bucketBitsSize = bucketSize << 3;
        // bits count per bucket element (folded by JIT to a constant 32 or 64 respectively)
        var blockBitsSize = nuint.Size << 3;
        // blocks count per bucket (use unsigned math so JIT will emit simple logical shift by 5 or 6 bits)
        var bucketBlocksSize = (int)((uint)bucketSize / (uint)nuint.Size);
        var lastBucketBitsOffset = 0x10000 - bucketBitsSize;

        for (var offset = 0; ; offset += bucketBitsSize)
        {
            lock (bucket)
            {
                var pool = bucket.Storage;

                for (var blockIndex = 0; blockIndex < pool.Length; blockIndex++)
                {
                    var bits = pool[blockIndex];
                    if (bits == nuint.MaxValue) continue;
                    var bitIndex = BitOperations.TrailingZeroCount(~bits);
                    pool[blockIndex] = bits | (nuint)0x1 << bitIndex;
                    return (ushort)(offset + blockIndex * blockBitsSize + bitIndex);
                }

                if (offset >= lastBucketBitsOffset)
                {
                    break;
                }

                bucket.Next ??= new(bucketBlocksSize);
            }

            bucket = bucket.Next;
        }

        ThrowRunOutOfIdentifiers();

        return 0;
    }

    public override void Return(ushort identifier)
    {
        var bitsSize = bucketSize << 3;
        var (bucketIndex, reminder) = int.DivRem(identifier, bitsSize);
        var (byteIndex, bitIndex) = int.DivRem(reminder, 8 * nuint.Size);

        var bucket = first;

        if (identifier == 0)
            ThrowIdIsNotTracked(identifier);

        for (var i = 0; i < bucketIndex; i++)
        {
            bucket = bucket.Next;
            if (bucket is not null) continue;
            ThrowIdIsNotTracked(identifier);
            return;
        }

        lock (bucket)
        {
            var block = bucket.Storage[byteIndex];
            var mask = (nuint)0x1 << bitIndex;
            if ((block & mask) == 0)
                ThrowIdIsNotTracked(identifier);
            bucket.Storage[byteIndex] = block & ~mask;
        }
    }

    [DoesNotReturn]
    private static void ThrowIdIsNotTracked(ushort identity) =>
        throw new InvalidOperationException($"Seems id '{identity}' is not tracked by this pool. Check your code for consistency.");

    [DoesNotReturn]
    private static void ThrowRunOutOfIdentifiers() =>
        throw new InvalidOperationException("Ran out of available identifiers.");

    private sealed class Bucket
    {
        public readonly nuint[] Storage;
        public Bucket? Next;

        public Bucket(int length) => Storage = new nuint[length];
    }
}