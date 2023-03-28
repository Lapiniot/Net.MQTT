namespace System.Net.Mqtt.Benchmarks.IdentityPool;

#pragma warning disable CA1822, CA1812

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class FastIdentityPoolBenchmarks
{
    public static IEnumerable<ushort> BucketSizes { get; } = new ushort[] { 32, 512 };
    public static IEnumerable<ushort> Rents { get; } = new ushort[] { 32, 512, 65535 };

    [ParamsSource(nameof(Rents))]
    public int RentCount { get; set; }

    [ParamsSource(nameof(BucketSizes))]
    public short BucketSize { get; set; }

    [Benchmark(Baseline = true)]
    public void RentParallelV1()
    {
        var pool = new FastIdentityPoolV1(BucketSize);

        Parallel.For(0, RentCount, new() { MaxDegreeOfParallelism = 8 }, _ => pool.Rent());
    }

    [Benchmark]
    public void RentParallelNext()
    {
        var pool = new FastIdentityPool(BucketSize);

        Parallel.For(0, RentCount, new() { MaxDegreeOfParallelism = 8 }, _ => pool.Rent());
    }
}