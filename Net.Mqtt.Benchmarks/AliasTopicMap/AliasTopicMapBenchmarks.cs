using AliasTopicMapNaive = Net.Mqtt.Benchmarks.AliasTopicMap.AliasTopicMap;
using AliasTopicMapOptimized = Net.Mqtt.AliasTopicMap;

namespace Net.Mqtt.Benchmarks.AliasTopicMap;

[MemoryDiagnoser]
public class AliasTopicMapBenchmarks
{
    private (ushort, ReadOnlyMemory<byte>)[] data =
    [
        ( 1, "Lorem ipsum dolor sit amet, consectetur adipiscing elit"u8.ToArray() ),
        ( 7, "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua"u8.ToArray() ),
        ( 3, "Ut enim ad minim veniam"u8.ToArray() ),
        ( 5, "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat"u8.ToArray() ),
        ( 5, default ),
        ( 3, "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum"u8.ToArray() ),
        ( 1, "dolore eu fugiat nulla pariatur"u8.ToArray() ),
        ( 7, default ),
        ( 5, "Excepteur sint occaecat cupidatat non proident"u8.ToArray() ),
        ( 7, "sunt in culpa qui officia deserunt mollit anim id est laborum"u8.ToArray()),
        ( 3, default ),
        ( 1, default ),
        ( 2, "Quis blandit turpis cursus in hac habitasse"u8.ToArray() ),
        ( 10, "Tristique senectus et netus et malesuada fames ac"u8.ToArray() ),
        ( 10, default )
    ];
    private AliasTopicMapNaive naiveImpl;
    private AliasTopicMapOptimized optimizedImpl;

    [Benchmark(Baseline = true)]
    public void GetOrUpdateTopicNaive()
    {
        naiveImpl.Initialize(ushort.MaxValue);
        foreach (var (alias, topic) in data)
        {
            var t = topic;
            naiveImpl.GetOrUpdateTopic(alias, ref t);
        }
    }

    [Benchmark]
    public void GetOrUpdateTopicOptimized()
    {
        optimizedImpl.Initialize(ushort.MaxValue);
        foreach (var (alias, topic) in data)
        {
            var t = topic;
            optimizedImpl.GetOrUpdateTopic(alias, ref t);
        }
    }
}