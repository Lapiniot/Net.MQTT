using System.Collections.Concurrent;

namespace Net.Mqtt.Benchmarks.ParallelEnumeration;

[MemoryDiagnoser]
[IterationCount(100)]
public class DictionaryBenchmark
{
    private ConcurrentDictionary<int, TaskCompletionSource> data = [];

    [Params(20, 200, 1000, 10000, 30000)]
    public int Count { get; set; }

    [IterationSetup]
    public void Setup()
    {
        data = [];
        for (var i = 0; i < Count; i++)
        {
            data[i] = new TaskCompletionSource();
        }
    }

    [Benchmark]
    public void SequentialForeach()
    {
        foreach (var item in data)
        {
            item.Value.TrySetCanceled();
        }
    }

    [Benchmark]
    public void ParallelForeach() => Parallel.ForEach(data, static item => item.Value.TrySetCanceled());

    [Benchmark]
    public void ParallelLinqDefault() => data.AsParallel().AsUnordered().WithMergeOptions(ParallelMergeOptions.Default).ForAll(static item => item.Value.TrySetCanceled());

    [Benchmark]
    public void ParallelLinqAutoBuffered() => data.AsParallel().AsUnordered().WithMergeOptions(ParallelMergeOptions.AutoBuffered).ForAll(static item => item.Value.TrySetCanceled());

    [Benchmark]
    public void ParallelLinqNotBuffered() => data.AsParallel().AsUnordered().WithMergeOptions(ParallelMergeOptions.NotBuffered).ForAll(static item => item.Value.TrySetCanceled());

    [Benchmark]
    public void ParallelLinqFullyBuffered() => data.AsParallel().AsUnordered().WithMergeOptions(ParallelMergeOptions.FullyBuffered).ForAll(static item => item.Value.TrySetCanceled());
}