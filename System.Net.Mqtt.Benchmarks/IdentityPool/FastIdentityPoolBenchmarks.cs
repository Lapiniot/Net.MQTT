namespace System.Net.Mqtt.Benchmarks.IdentityPool;

#pragma warning disable CA1822, CA1812

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
public class FastIdentityPoolBenchmarks
{
    [Benchmark(Baseline = true)]
    public void RentParallelV1()
    {
        var pool = new FastIdentityPoolV1();

        Parallel.For(0, 65535, new() { MaxDegreeOfParallelism = 8 }, _ => pool.Rent());
    }

    [Benchmark]
    public void RentParallelNext()
    {
        var pool = new FastIdentityPool();

        Parallel.For(0, 65535, new() { MaxDegreeOfParallelism = 8 }, _ => pool.Rent());
    }
}