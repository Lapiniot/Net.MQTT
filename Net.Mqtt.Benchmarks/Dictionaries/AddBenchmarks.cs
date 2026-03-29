using System.Collections.Frozen;
using System.Collections.Immutable;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Benchmarks.Dictionaries;

public class AddBenchmarks : BenchmarksBase
{
    private readonly Dictionary<byte[], SubscriptionOptions> dictionary = new(ByteSequenceComparer.Instance);
    private readonly ImmutableDictionary<byte[], SubscriptionOptions> immutable = [];
    private readonly FrozenDictionary<byte[], SubscriptionOptions> frozen = FrozenDictionary<byte[], SubscriptionOptions>.Empty;

    [Benchmark(Baseline = true)]
    public void DictionaryAdd()
    {
        var snapshot = dictionary;
        foreach (var (key, value) in Data)
        {
            snapshot = snapshot.ToDictionary(ByteSequenceComparer.Instance);
            snapshot.Add(key, value);
        }
    }

    [Benchmark]
    public void DictionaryAddBatch()
    {
        var cloned = dictionary.ToDictionary(ByteSequenceComparer.Instance);
        foreach (var (key, value) in Data)
        {
            cloned.Add(key, value);
        }
    }

    [Benchmark]
    public void ImmutableDictionaryAdd()
    {
        var snapshot = immutable;
        foreach (var (key, value) in Data)
        {
            snapshot = snapshot.Add(key, value);
        }
    }

    [Benchmark]
    public void ImmutableDictionaryAddBatch()
    {
        var builder = immutable.ToBuilder();
        foreach (var (key, value) in Data)
        {
            builder.Add(key, value);
        }

        _ = builder.ToImmutable();
    }

    [Benchmark]
    public void FrozenDictionaryAdd()
    {
        var snapshot = frozen;
        foreach (var (key, value) in Data)
        {
            var cloned = snapshot.ToDictionary(ByteSequenceComparer.Instance);
            cloned.Add(key, value);
            snapshot = cloned.ToFrozenDictionary(ByteSequenceComparer.Instance);
        }
    }

    [Benchmark]
    public void FrozenDictionaryAddBatch()
    {
        var cloned = frozen.ToDictionary(ByteSequenceComparer.Instance);
        foreach (var (key, value) in Data)
        {
            cloned.Add(key, value);
        }

        _ = cloned.ToFrozenDictionary(ByteSequenceComparer.Instance);
    }
}