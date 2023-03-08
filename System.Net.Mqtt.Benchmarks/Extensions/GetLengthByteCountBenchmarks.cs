using V10 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV10;
using Next = System.Net.Mqtt.Extensions.MqttExtensions;

#pragma warning disable CA1822, CA1812

namespace System.Net.Mqtt.Benchmarks.Extensions;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
public class GetLengthByteCountBenchmarks
{
    private static readonly int[] Data = { 0, 100, 127, 128, 16000, 16383, 16384, 2097000, 2097151, 2097152, 268435000, 268435455 };

    [Benchmark(Baseline = true)]
    [MethodImpl(NoOptimization)]
    public void GetLengthByteCountV1()
    {
        var span = Data.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V10.GetLengthByteCount(span[i]);
        }
    }

    [Benchmark]
    [MethodImpl(NoOptimization)]
    public void GetLengthByteCountV2()
    {
        var span = Data.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            Next.GetLengthByteCount(span[i]);
        }
    }
}