using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Benchmarks.Dictionaries;

[MemoryDiagnoser(false)]
public class EnumerationBenchmarks : BenchmarksBase
{
    private Dictionary<byte[], SubscriptionOptions> dictionary;
    private FrozenDictionary<byte[], SubscriptionOptions> frozen;
    private ImmutableDictionary<byte[], SubscriptionOptions> immutable;

    public override void Setup()
    {
        base.Setup();

        dictionary = Data;
        frozen = Data.ToFrozenDictionary(ByteSequenceComparer.Instance);
        immutable = Data.ToImmutableDictionary(ByteSequenceComparer.Instance);
    }

    [Benchmark(Baseline = true)]
    public void DictionaryEnumerator()
    {
        long sum = 0;
        foreach (var (key, value) in dictionary)
        {
            sum += value.QoS;
        }
    }

    [Benchmark]
    public void FrozenDictionaryEnumeratorKVP()
    {
        long sum = 0;
        foreach (var (key, value) in frozen)
        {
            sum += value.QoS;
        }
    }

    [Benchmark]
    public void FrozenDictionaryKeysValuesByIndex()
    {
        long sum = 0;
        var keys = frozen.Keys;
        var values = frozen.Values;
        for (var i = 0; i < keys.Length; i++)
        {
            _ = keys[i];
            var value = values[i];
            sum += value.QoS;
        }
    }

    [Benchmark]
    public void FrozenDictionaryKeysValuesByIndexAsArray()
    {
        long sum = 0;
        var keys = ImmutableCollectionsMarshal.AsArray(frozen.Keys);
        var values = ImmutableCollectionsMarshal.AsArray(frozen.Values);
        for (var i = 0; i < keys.Length; i++)
        {
            _ = keys[i];
            var value = values[i];
            sum += value.QoS;
        }
    }

    [Benchmark]
    public void FrozenDictionaryKeysValuesByIndexAsSpan()
    {
        long sum = 0;
        var keys = frozen.Keys.AsSpan();
        var values = frozen.Values.AsSpan();
        for (var i = 0; i < keys.Length; i++)
        {
            _ = keys[i];
            var value = values[i];
            sum += value.QoS;
        }
    }

    [Benchmark]
    public void FrozenDictionaryKeysValuesByIndexAsSpanByRef()
    {
        long sum = 0;
        var keys = frozen.Keys.AsSpan();
        var values = frozen.Values.AsSpan();
        for (var i = 0; i < keys.Length; i++)
        {
            _ = keys[i];
            ref readonly var value = ref values[i];
            sum += value.QoS;
        }
    }

    [Benchmark]
    public void ImmutableDictionaryEnumerator()
    {
        long sum = 0;
        foreach (var (key, value) in immutable)
        {
            sum += value.QoS;
        }
    }
}