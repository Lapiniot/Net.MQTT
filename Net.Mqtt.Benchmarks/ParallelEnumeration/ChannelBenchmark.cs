using System.Threading.Channels;

namespace Net.Mqtt.Benchmarks.ParallelEnumeration;

[MemoryDiagnoser]
[IterationCount(100)]
public class ChannelBenchmark
{
    private Channel<TaskCompletionSource> data;

    [Params(20, 200, 1000, 10000, 30000)]
    public int Count { get; set; }

    [IterationSetup]
    public void Setup()
    {
        data = Channel.CreateUnbounded<TaskCompletionSource>();
        var writer = data.Writer;
        for (var i = 0; i < Count; i++)
        {
            writer.TryWrite(new TaskCompletionSource());
        }

        writer.Complete();
    }

    [Benchmark]
    public void Sequential()
    {
        var reader = data.Reader;
        while (reader.TryRead(out var tcs))
        {
            tcs.TrySetCanceled();
        }
    }

    [Benchmark]
    public async Task ReadAllAsync()
    {
        await foreach (var item in data.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            item.TrySetCanceled();
        }
    }

    [Benchmark]
    public async Task ParallelReadAllAsync()
    {
        await Parallel.ForEachAsync(data.Reader.ReadAllAsync(), (cts, ct) =>
        {
            cts.TrySetCanceled(ct);
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
    }
}