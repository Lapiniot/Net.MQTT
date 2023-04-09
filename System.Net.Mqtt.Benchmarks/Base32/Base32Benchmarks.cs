namespace System.Net.Mqtt.Benchmarks.Base32;

#pragma warning disable CA1822

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class Base32Benchmarks
{
    [Benchmark(Baseline = true)]
    [MethodImpl(NoOptimization)]
    public void ToBase32StringV1() => Base32V1.ToBase32String(long.MaxValue);

    [Benchmark]
    [MethodImpl(NoOptimization)]
    public void ToBase32StringNext() => System.Base32.ToBase32String(long.MaxValue);
}