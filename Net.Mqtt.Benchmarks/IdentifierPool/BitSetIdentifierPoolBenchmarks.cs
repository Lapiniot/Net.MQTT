namespace Net.Mqtt.Benchmarks.IdentifierPool;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class BitSetIdentifierPoolBenchmarks
{
    private BitSetIdentifierPoolV1 poolV1;
    private BitSetIdentifierPool poolNext;

    public static IEnumerable<short> BucketSizeParamValues { get; } = [512];
    public static IEnumerable<int> RentParamValues { get; } = [65535];
    public static IEnumerable<int> MdopParamValues { get; } = [1, /*Environment.ProcessorCount / 2*/];

    [ParamsSource(nameof(MdopParamValues))]
    public int MDOP { get; set; }

    [ParamsSource(nameof(RentParamValues))]
    public int Rents { get; set; }

    [ParamsSource(nameof(BucketSizeParamValues))]
    public short BucketSize { get; set; }

    [IterationSetup(Target = nameof(ReturnParallelV1))]
    public void SetupForReturnParallelV1()
    {
        poolV1 = new BitSetIdentifierPoolV1(BucketSize);
        for (var i = 0; i < Rents; i++) _ = poolV1.Rent();
    }

    [IterationSetup(Targets = [nameof(ReturnParallelNext)])]
    public void SetupForReturnParallelNext()
    {
        poolNext = new BitSetIdentifierPool(BucketSize);
        for (var i = 0; i < Rents; i++) _ = poolNext.Rent();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IdentifierPool-Rent-Parallel")]
    public void RentParallelV1()
    {
        var pool = new BitSetIdentifierPoolV1(BucketSize);

        Parallel.For(0, Rents, new() { MaxDegreeOfParallelism = MDOP }, _ => pool.Rent());
    }

    [Benchmark]
    [BenchmarkCategory("IdentifierPool-Rent-Parallel")]
    public void RentParallelNext()
    {
        var pool = new BitSetIdentifierPool(BucketSize);

        Parallel.For(0, Rents, new()
        {
            MaxDegreeOfParallelism = MDOP
        }, _ => pool.Rent());
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IdentifierPool-Return-Parallel")]
    public void ReturnParallelV1() =>
        Parallel.For(0, Rents, new() { MaxDegreeOfParallelism = MDOP }, id => poolV1.Return((ushort)(id + 1)));

    [Benchmark]
    [BenchmarkCategory("IdentifierPool-Return-Parallel")]
    public void ReturnParallelNext() =>
        Parallel.For(0, Rents, new() { MaxDegreeOfParallelism = MDOP }, id => poolNext.Return((ushort)(id + 1)));
}