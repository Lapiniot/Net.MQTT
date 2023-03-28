namespace System.Net.Mqtt.Benchmarks.IdentityPool;

#pragma warning disable CA1822, CA1812

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class FastIdentityPoolBenchmarks
{
    private FastIdentityPoolV1 poolV1;
    private FastIdentityPool poolNext;

    public static IEnumerable<ushort> BucketSizes { get; } = new ushort[] { 32, 128, 512 };
    public static IEnumerable<ushort> Rents { get; } = new ushort[] { 65535 };

    [ParamsSource(nameof(Rents))]
    public int RentCount { get; set; }

    [ParamsSource(nameof(BucketSizes))]
    public short BucketSize { get; set; }

    [IterationSetup(Target = nameof(ReturnParallelV1))]
    public void SetupForReturnParallelV1()
    {
        poolV1 = new FastIdentityPoolV1(BucketSize);
        Parallel.For(0, RentCount, new() { MaxDegreeOfParallelism = 8 }, _ => poolV1.Rent());
    }

    [IterationSetup(Target = nameof(ReturnParallelNext))]
    public void SetupForReturnParallelNext()
    {
        poolNext = new FastIdentityPool(BucketSize);
        Parallel.For(0, RentCount, new() { MaxDegreeOfParallelism = 8 }, _ => poolNext.Rent());
    }

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

    [Benchmark(Baseline = true)]
    public void ReturnParallelV1() =>
        Parallel.For(0, RentCount, new() { MaxDegreeOfParallelism = 8 }, id => poolV1.Release((ushort)(id + 1)));

    [Benchmark]
    public void ReturnParallelNext() =>
        Parallel.For(0, RentCount, new() { MaxDegreeOfParallelism = 8 }, id => poolNext.Release((ushort)(id + 1)));
}