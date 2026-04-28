using System.Text;

namespace Net.Mqtt.Benchmarks.Dictionaries;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[MemoryDiagnoser(true)]
public abstract class BenchmarksBase
{
    [Params(10, 100)]
    public int Count { get; set; }

    protected Dictionary<byte[], Subscription> Data { get; private set; }

    [GlobalSetup]
    public virtual void Setup() => Data = GenerateTestData(Count);

    private static Dictionary<byte[], Subscription> GenerateTestData(int size)
    {
        var dictionary = new Dictionary<byte[], Subscription>(size, ByteSequenceComparer.Instance);
        for (var i = 0; i < size; i++)
        {
            dictionary[Encoding.UTF8.GetBytes($"key{i}")] = default;
        }

        return dictionary;
    }
}

public record struct Subscription(byte QoS, byte Flags, uint SubscriptionId);