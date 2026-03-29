using System.Collections.Frozen;
using System.Collections.Immutable;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Benchmarks.Dictionaries;

public class UpdateBenchmarks : BenchmarksBase
{
    private Dictionary<byte[], SubscriptionOptions> dictionary;
    private FrozenDictionary<byte[], SubscriptionOptions> frozen;
    private ImmutableDictionary<byte[], SubscriptionOptions> immutable;

    public override void Setup()
    {
        base.Setup();

        dictionary = Data.ToDictionary(ByteSequenceComparer.Instance);
        immutable = Data.ToImmutableDictionary(ByteSequenceComparer.Instance);
        frozen = Data.ToFrozenDictionary(ByteSequenceComparer.Instance);
    }

    [Benchmark(Baseline = true)]
    public void DictionaryUpdate()
    {
        var snapshot = dictionary;
        foreach (var (key, value) in Data)
        {
            snapshot = snapshot.ToDictionary(ByteSequenceComparer.Instance);
            snapshot[key] = value with { QoS = 2, SubscriptionId = 10 };
        }
    }

    [Benchmark]
    public void DictionaryUpdateBatch()
    {
        var cloned = dictionary.ToDictionary(ByteSequenceComparer.Instance);
        foreach (var (key, value) in Data)
        {
            cloned[key] = value with { QoS = 2, SubscriptionId = 10 };
        }
    }

    [Benchmark]
    public void ImmutableDictionaryUpdate()
    {
        var snapshot = immutable;
        foreach (var (key, value) in Data)
        {
            snapshot = snapshot.SetItem(key, value with { QoS = 2, SubscriptionId = 10 });
        }
    }

    [Benchmark]
    public void ImmutableDictionaryUpdateBatch()
    {
        var builder = immutable.ToBuilder();
        foreach (var (key, value) in Data)
        {
            builder[key] = value with { QoS = 2, SubscriptionId = 10 };
        }

        _ = builder.ToImmutable();
    }

    [Benchmark]
    public void FrozenDictionaryUpdate()
    {
        var snapshot = frozen;
        foreach (var (key, value) in Data)
        {
            var cloned = snapshot.ToDictionary(ByteSequenceComparer.Instance);
            cloned[key] = value with { QoS = 2, SubscriptionId = 10 };
            snapshot = cloned.ToFrozenDictionary(ByteSequenceComparer.Instance);
        }
    }

    [Benchmark]
    public void FrozenDictionaryUpdateBatch()
    {
        var cloned = frozen.ToDictionary(ByteSequenceComparer.Instance);
        foreach (var (key, value) in Data)
        {
            cloned[key] = value with { QoS = 2, SubscriptionId = 10 };
        }

        _ = cloned.ToFrozenDictionary(ByteSequenceComparer.Instance);
    }
}