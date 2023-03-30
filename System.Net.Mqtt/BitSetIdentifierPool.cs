using System.Runtime.InteropServices;

namespace System.Net.Mqtt;

#nullable enable

/// <summary>
/// Implements fast concurrent id pool, which uses contiguous arrays and direct indexing to maintain state
/// </summary>
public sealed class BitSetIdentifierPool : IdentifierPool
{
    private const ushort MinBucketSize = 16;
    private const ushort MaxBucketSize = 8192;
    private const ushort DefaultBucketSize = 32;
    private readonly int bucketBitSize;
    private readonly int bucketBitSizeShift;
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

        var bucketBitSize = bucketSize << 3;
        this.bucketBitSize = bucketBitSize;
        // precalculate and store log2(bucketBitSize) in the field to be used for future bit-shift 
        // divission operations, since LZCNT e.g. is fairly expensive when calculated on each run
        bucketBitSizeShift = 31 ^ BitOperations.LeadingZeroCount((uint)bucketBitSize);

        first = new((int)((uint)bucketSize / (uint)nuint.Size));
        // 0 - is invalid packet id according to MQTT spec, so reserve it right away
        first.Storage[0] = 0x1;
    }

    public override ushort Rent()
    {
        var bucket = first;

        var bitsSize = bucketBitSize;
        // bits count per bucket element (folded by JIT to a constant 32 or 64 respectively)
        var blockBitsSize = nuint.Size << 3;
        // blocks count per bucket (use unsigned math so JIT will emit simple logical shift by 5 or 6 bits)
        var bucketBlocksSize = (int)((uint)bitsSize / (uint)(nuint.Size * 8));
        var lastBucketBitsOffset = 0x10000 - bitsSize;

        for (var offset = 0; ; offset += bitsSize)
        {
            lock (bucket)
            {
                var blocks = bucket.Storage;

                for (var blockOffset = 0; blockOffset < blocks.Length; blockOffset++)
                {
                    var block = blocks[blockOffset];
                    if (block == nuint.MaxValue) continue;
                    var bitOffset = BitOperations.TrailingZeroCount(~block);
                    blocks[blockOffset] = block | (nuint)0x1 << bitOffset;
                    return (ushort)(offset + blockOffset * blockBitsSize + bitOffset);
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
        var bucketNumber = identifier >>> bucketBitSizeShift;
        var bucketBitOffset = identifier & (bucketBitSize - 1);
        var blockOffset = (int)((uint)bucketBitOffset / (uint)(8 * nuint.Size));
        var bitOffset = bucketBitOffset & (8 * nuint.Size - 1);
        var bucket = first;

        if (identifier == 0)
            ThrowInvalidId(identifier);

        for (var i = 0; i < bucketNumber; i++)
        {
            bucket = bucket.Next;
            if (bucket is null)
                ThrowInvalidId(identifier);
        }

        lock (bucket)
        {
            // Use managed ref directly in order to eliminate array bounds check since we now index will 
            // never go out of range (according to our calculations we know its impossible, but JIT cannot figure it out)
            ref var block = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bucket.Storage), blockOffset);
            if ((block & ((nuint)1 << bitOffset)) == 0)
                ThrowInvalidId(identifier);
            block &= ~((nuint)1 << bitOffset);
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidId(ushort identity) =>
        throw new InvalidOperationException($"Seems id '{identity}' was not 'borrowed' legally from this pool. Check your code for consistency.");

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