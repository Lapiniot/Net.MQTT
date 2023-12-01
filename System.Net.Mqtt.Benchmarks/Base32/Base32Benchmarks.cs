namespace System.Net.Mqtt.Benchmarks.Base32;

#pragma warning disable CA1822

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[MemoryDiagnoser]
[WarmupCount(50)]
public class Base32Benchmarks
{
#pragma warning disable CA5394 // Do not use insecure randomness
    private static readonly long Value = Random.Shared.NextInt64();
#pragma warning restore CA5394 // Do not use insecure randomness

    [Benchmark(Baseline = true)]
    public void ToBase32StringV1() => Base32V1.ToBase32String(Value);

    [Benchmark]
    public void ToBase32StringV2() => Base32V2.ToBase32String(Value);

    [Benchmark]
    public void ToBase32StringNext() => System.Base32.ToBase32String(Value);
}