using System.Diagnostics;
using System.Net.Mqtt;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    private const int MaxProgressWidth = 120;

    internal static async Task GenericTestAsync(MqttClientBuilder clientBuilder, TestProfile profile, int numConcurrent,
        Func<MqttClient, int, CancellationToken, Task> testCore,
        Func<double> getCurrentProgress,
        Func<MqttClient, int, CancellationToken, Task> setupClient,
        Func<MqttClient, int, CancellationToken, Task> cleanupClient,
        Func<CancellationToken, Task> finalizeTest)
    {
        var (_, _, numClients, _, timeout, updateInterval, noProgress, _) = profile;
        using var cts = new CancellationTokenSource(timeout);
        var cancellationToken = cts.Token;

        var clients = new List<MqttClient>();
        for(int i = 0; i < numClients; i++)
        {
            clients.Add(clientBuilder.BuildV4());
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;

        await ConnectAllAsync(clients, cancellationToken).ConfigureAwait(false);

        await RunAllAsync(clients, setupClient, numConcurrent, cancellationToken).ConfigureAwait(false);

        var stopwatch = new Stopwatch();

        using var updateProgressCts = new CancellationTokenSource();
        try
        {
            if(!noProgress)
            {
                _ = UpdateProgressAsync(updateProgressCts.Token);
            }

            stopwatch.Start();
            await RunAllAsync(clients, testCore, numConcurrent, cancellationToken).ConfigureAwait(false);
            await finalizeTest(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            RenderReport(stopwatch.Elapsed);
        }
        finally
        {
            updateProgressCts.Cancel();
            stopwatch.Stop();
            await RunAllAsync(clients, cleanupClient, numConcurrent, cancellationToken).ConfigureAwait(false);
            await DisconnectAllAsync(clients).ConfigureAwait(false);
        }

        async Task UpdateProgressAsync(CancellationToken cancellationToken)
        {
            RenderProgress(0);
            using var timer = new PeriodicTimer(updateInterval);
            while(await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                RenderProgress(getCurrentProgress());
            }
        }
    }

    private static void RenderTestSettings(string testName, int numClients, int numMessages, QoSLevel qosLevel, int maxConcurrent)
    {
        Console.WriteLine(@$"
Starting concurrent '{testName}' test...

Connected clients:      {numClients}
Concurrent clients:     {maxConcurrent}
Messages per client:    {numMessages}
QoS level:              {qosLevel}
");
    }

    private static void RenderReport(TimeSpan elapsed)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"{{0,-{Console.WindowWidth}}}", new string('-', Math.Min(Console.WindowWidth, 70)));
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("Elapsed time: {0:hh\\:mm\\:ss\\.fff} ({1:N2} ms.)\n", elapsed, elapsed.TotalMilliseconds);
    }

    private static void RenderProgress(double progress)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        var maxWidth = Math.Min(MaxProgressWidth, Console.WindowWidth);
        var progressWidth = maxWidth - 12;
        var dots = (int)(progressWidth * progress);
        Console.Write("[");
        for(int i = 0; i < dots; i++) Console.Write("#");
        for(int i = 0; i < progressWidth - dots; i++) Console.Write(".");
        Console.Write("]");
        Console.Write("{0,8:P2}", progress);
    }

    private static async Task ConnectAllAsync(List<MqttClient> clients, CancellationToken cancellationToken)
    {
        await Task.WhenAll(clients.Select(client => client.ConnectAsync(new MqttConnectionOptions(KeepAlive: 20), cancellationToken))).ConfigureAwait(false);
    }

    private static async Task DisconnectAllAsync(List<MqttClient> clients)
    {
        await Task.WhenAll(clients.Select(client => client.DisconnectAsync())).ConfigureAwait(false);
        await Task.WhenAll(clients.Select(client => client.DisposeAsync().AsTask())).ConfigureAwait(false);
    }

    private static async Task RunAllAsync(List<MqttClient> clients, Func<MqttClient, int, CancellationToken, Task> func, int maxConcurrent, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrent);
        await Task.WhenAll(clients.Select(async (client, index) =>
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                await func(client, index, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        })).ConfigureAwait(false);
    }

    private static Task DoNothing(MqttClient client, int index, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
