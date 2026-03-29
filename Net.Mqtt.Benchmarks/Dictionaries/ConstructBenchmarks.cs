using System.Collections.Frozen;
using System.Collections.Immutable;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Benchmarks.Dictionaries;

public class ConstructBenchmarks : BenchmarksBase
{

    [Benchmark(Baseline = true)]
    public void RegularDictionaryClone()
    {
        var _ = new Dictionary<byte[], SubscriptionOptions>(Data, ByteSequenceComparer.Instance);
    }

    [Benchmark]
    public void RegularDictionaryPreallocated()
    {
        var dict = new Dictionary<byte[], SubscriptionOptions>(Data.Count, ByteSequenceComparer.Instance);
        foreach (var kvp in Data)
        {
            dict[kvp.Key] = kvp.Value;
        }
    }

    [Benchmark]
    public void ImmutableDictionaryCreateRange()
    {
        var _ = ImmutableDictionary.CreateRange(keyComparer: ByteSequenceComparer.Instance, items: Data);
    }

    [Benchmark]
    public void ImmutableDictionaryBuilder()
    {
        var builder = ImmutableDictionary.CreateBuilder<byte[], SubscriptionOptions>(keyComparer: ByteSequenceComparer.Instance);
        foreach (var kvp in Data)
        {
            builder[kvp.Key] = kvp.Value;
        }

        var _ = builder.ToImmutable();
    }

    [Benchmark]
    public void ImmutableDictionaryFromDictionary()
    {
        var _ = Data.ToImmutableDictionary(keyComparer: ByteSequenceComparer.Instance);
    }

    [Benchmark]
    public void FrozenDictionary()
    {
        var _ = Data.ToFrozenDictionary(comparer: ByteSequenceComparer.Instance);
    }
}