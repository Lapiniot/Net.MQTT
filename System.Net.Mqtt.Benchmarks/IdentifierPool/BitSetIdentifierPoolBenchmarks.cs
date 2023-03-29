namespace System.Net.Mqtt.Benchmarks.IdentifierPool;

#pragma warning disable CA1822, CA1812

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class BitSetIdentifierPoolBenchmarks
{
    private BitSetIdentifierPoolV1 poolV1;
    private BitSetIdentifierPool poolNext;

    public static IEnumerable<short> BucketSizeParamValues { get; } = new short[] { 512 };
    public static IEnumerable<int> RentParamValues { get; } = new[] { 65535 };
    public static IEnumerable<int> MdopParamValues { get; } = new[] { 1, /*Environment.ProcessorCount / 2*/ };

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

    [IterationSetup(Targets = new[] { nameof(ReturnParallelNext) })]
    public void SetupForReturnParallelNext()
    {
        poolNext = new BitSetIdentifierPool(BucketSize);
        for (var i = 0; i < Rents; i++) _ = poolNext.Rent();
    }

    [Benchmark(Baseline = true)]
    public void RentParallelV1()
    {
        var pool = new BitSetIdentifierPoolV1(BucketSize);

        Parallel.For(0, Rents, new() { MaxDegreeOfParallelism = MDOP }, _ => pool.Rent());
    }

    [Benchmark]
    public void RentParallelNext()
    {
        var pool = new BitSetIdentifierPool(BucketSize);

        Parallel.For(0, Rents, new() { MaxDegreeOfParallelism = MDOP }, _ => pool.Rent());
    }

    [Benchmark(Baseline = true)]
    public void ReturnParallelV1() =>
        Parallel.For(0, Rents, new() { MaxDegreeOfParallelism = MDOP }, id => poolV1.Return((ushort)(id + 1)));

    [Benchmark]
    public void ReturnParallelNext() =>
        Parallel.For(0, Rents, new() { MaxDegreeOfParallelism = MDOP }, id => poolNext.Return((ushort)(id + 1)));
}