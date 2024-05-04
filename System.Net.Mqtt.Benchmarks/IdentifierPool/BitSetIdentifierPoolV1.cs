namespace System.Net.Mqtt.Benchmarks.IdentifierPool;

#nullable enable

/// <summary>
/// Implements fast concurrent id pool, which uses contiguous arrays and direct indexing to maintain state
/// </summary>
public class BitSetIdentifierPoolV1 : Mqtt.IdentifierPool
{
    private const short MinBucketSize = 8;
    private const short MaxBucketSize = 8192;
    private const short DefaultBucketSize = 32;
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
    /// to the <see cref="Rent" /> without subsequent calls to <see cref="Return" /> make list growing, allocating
    /// more memory. However, normally, list is not expanded if rented ids are returned to the pool shortly.
    /// Also keep in mind, <paramref name="bucketSize" /> should be the power of 2 for performance reasons
    /// (in order to avoid fractions calculations).
    /// </remarks>
    public BitSetIdentifierPoolV1(short bucketSize = DefaultBucketSize)
    {
        Verify.ThrowIfNotInRange(bucketSize, MinBucketSize, MaxBucketSize);
        Verify.ThrowIfNotPowerOfTwo(bucketSize);

        this.bucketSize = bucketSize;
        first = new(bucketSize);
    }

    public override ushort Rent()
    {
        var bucket = first;
        var shift = 1; // used to skip over forbidden initial value 0
        var bitsSize = bucketSize << 3;

        for (var offset = 0; ; offset += bitsSize)
        {
            lock (bucket)
            {
                var pool = bucket.Storage;

                for (var byteIndex = 0; byteIndex < bucketSize; byteIndex++)
                {
                    var block = pool[byteIndex];
                    for (var bitIndex = shift; bitIndex < 8; bitIndex++)
                    {
                        var mask = 0x1 << bitIndex;
                        if ((block & mask) != 0) continue;
                        pool[byteIndex] = (byte)(block | mask);
                        return (ushort)(offset + (byteIndex << 3) + bitIndex);
                    }

                    shift = 0;
                }

                if (offset + bitsSize >= 0xFFFF)
                    break;

                bucket.Next ??= new(bucketSize);
            }

            bucket = bucket.Next;
        }

        ThrowRunOutOfIdentifiers();

        return 0;
    }

    public override void Return(ushort identifier)
    {
        var bitsSize = bucketSize << 3;
        var bucketIndex = identifier / bitsSize;
        var byteIndex = identifier % bitsSize >> 3;
        var bitIndex = identifier % bitsSize % 8;

        var bucket = first;

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
            var mask = 0x1 << bitIndex;
            if ((block & mask) == 0)
                ThrowIdIsNotTracked(identifier);
            bucket.Storage[byteIndex] = (byte)(block & ~mask);
        }
    }

    [DoesNotReturn]
    private static void ThrowIdIsNotTracked(ushort identity) =>
        throw new InvalidOperationException($"Seems id '{identity}' is not tracked by this pool. Check your code for consistency.");

    [DoesNotReturn]
    private static void ThrowRunOutOfIdentifiers() =>
        throw new InvalidOperationException("Ran out of available identifiers.");

    private sealed class Bucket(short size)
    {
        public readonly byte[] Storage = new byte[size];
        public Bucket? Next;
    }
}