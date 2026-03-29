using System.Text;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Benchmarks.Dictionaries;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[MemoryDiagnoser(true)]
public abstract class BenchmarksBase
{
    [Params(10, 100)]
    public int Count { get; set; }

    protected Dictionary<byte[], SubscriptionOptions> Data { get; private set; }

    [GlobalSetup]
    public virtual void Setup() => Data = GenerateTestData(Count);

    private static Dictionary<byte[], SubscriptionOptions> GenerateTestData(int size)
    {
        var dictionary = new Dictionary<byte[], SubscriptionOptions>(size, ByteSequenceComparer.Instance);
        for (var i = 0; i < size; i++)
        {
            dictionary[Encoding.UTF8.GetBytes($"key{i}")] = default;
        }

        return dictionary;
    }
}