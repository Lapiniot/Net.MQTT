namespace System.Net.Mqtt.Benchmarks.OrderedHashMap;

#pragma warning disable CA2227

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class OrderedHashMapBenchmarks
{
    private Dictionary<string, string> sampledData;
    private OrderedHashMapV1<string, string> mapV1;
    private OrderedHashMap<string, string> map;

    private static Dictionary<string, string> GetSampleData(int count)
    {
        Dictionary<string, string> dictionary = [];
        for (var i = 0; i < count; i++)
            dictionary["sample-key-" + i] = "sample-value-" + i;
        return dictionary;
    }

    [Params(500, 5000, 50000)]
    public static int Count { get; set; }

    [ParamsAllValues]
    public static Mode Mode { get; set; }

    [IterationSetup(Target = nameof(AddOrUpdateV1))]
    public void SetupForAddOrUpdateV1()
    {
        sampledData = GetSampleData(Count);
        mapV1 = Mode is Mode.Add ? new() : new(sampledData);
    }

    [IterationSetup(Target = nameof(AddOrUpdateCurrent))]
    public void SetupForAddOrUpdateCurrent()
    {
        sampledData = GetSampleData(Count);
        map = Mode is Mode.Add ? new() : new(sampledData);
    }

    [Benchmark(Baseline = true)]
    [MethodImpl(AggressiveOptimization)]
    public void AddOrUpdateV1() => Parallel.ForEach(sampledData, p => mapV1.AddOrUpdate(p.Key, p.Value));

    [Benchmark]
    [MethodImpl(AggressiveOptimization)]
    public void AddOrUpdateCurrent() => Parallel.ForEach(sampledData, p => map.AddOrUpdate(p.Key, p.Value));
}

public enum Mode
{
    Add,
    Update
}