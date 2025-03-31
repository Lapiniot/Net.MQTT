namespace Mqtt.Benchmark;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
internal readonly record struct CancelAwaitableScope : IAsyncDisposable
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private readonly CancellationTokenSource cts;
    private readonly Task workerTask;

    public CancelAwaitableScope(Func<CancellationToken, Task> worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        cts = new CancellationTokenSource();
        workerTask = worker(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        using (cts)
        {
            await cts.CancelAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            await workerTask.ConfigureAwait(false);
        }
    }
}