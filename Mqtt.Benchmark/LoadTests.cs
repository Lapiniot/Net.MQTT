using System.Buffers;
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
        Func<MqttClient, int, CancellationToken, Task> setupClient = null,
        Func<MqttClient, int, CancellationToken, Task> cleanupClient = null,
        Func<CancellationToken, Task> finalizeTest = null,
        CancellationToken stoppingToken = default)
    {
        var (_, _, numClients, _, _, timeout, updateInterval, noProgress, _, _, _) = profile;
        using var cts = new CancellationTokenSource(timeout);
        using var jointCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
        var cancellationToken = jointCts.Token;

        var clients = new List<MqttClient>();
        for (var i = 0; i < numClients; i++)
        {
            clients.Add(clientBuilder.BuildV4());
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;

        await ConnectAllAsync(clients, cancellationToken).ConfigureAwait(false);

        if (setupClient is not null)
        {
            await RunAllAsync(clients, setupClient, numConcurrent, cancellationToken).ConfigureAwait(false);
        }

        var stopwatch = new Stopwatch();

        using var updateProgressCts = new CancellationTokenSource();
        try
        {
            if (!noProgress)
            {
                _ = UpdateProgressAsync(updateProgressCts.Token);
            }

            stopwatch.Start();
            await RunAllAsync(clients, testCore, numConcurrent, cancellationToken).ConfigureAwait(false);

            if (finalizeTest is not null)
            {
                await finalizeTest(cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();
            RenderReport(stopwatch.Elapsed);
        }
        finally
        {
            updateProgressCts.Cancel();
            stopwatch.Stop();

            if (cleanupClient is not null)
            {
                await RunAllAsync(clients, cleanupClient, numConcurrent, CancellationToken.None).ConfigureAwait(false);
            }

            await DisconnectAllAsync(clients).ConfigureAwait(false);
        }

        async Task UpdateProgressAsync(CancellationToken token)
        {
            RenderProgress(0);
            using var timer = new PeriodicTimer(updateInterval);
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                RenderProgress(getCurrentProgress());
            }
        }
    }

    private static async Task PublishAsync(MqttClient client, int clientIndex, QoSLevel qosLevel, int minPayloadSize, int maxPayloadSize, string testId, int messageIndex, CancellationToken token)
    {
#pragma warning disable CA5394
        var length = Random.Shared.Next(minPayloadSize, maxPayloadSize);
#pragma warning restore CA5394
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var payload = new ReadOnlyMemory<byte>(buffer)[..length];
            await client.PublishAsync($"TEST-{testId}/CLIENT-{clientIndex:D6}/MSG-{messageIndex:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void RenderTestSettings(string testName, int numClients, int numMessages, QoSLevel qosLevel, int maxConcurrent) =>
        Console.WriteLine(@$"
Starting{(numClients > 1 ? " concurrent" : "")} '{testName}' test...

Connected clients:          {numClients}
Concurrent clients:         {maxConcurrent}
Messages per client:        {numMessages}
QoS level:                  {qosLevel}");

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
        for (var i = 0; i < dots; i++) Console.Write("#");
        for (var i = 0; i < progressWidth - dots; i++) Console.Write(".");
        Console.Write("]");
        Console.Write("{0,8:P2}", progress);
    }

    private static async Task ConnectAllAsync(IEnumerable<MqttClient> clients, CancellationToken cancellationToken) =>
        await Task.WhenAll(clients.Select(client => client.ConnectAsync(new(KeepAlive: 20), cancellationToken))).ConfigureAwait(false);

    private static async Task DisconnectAllAsync(IReadOnlyCollection<MqttClient> clients)
    {
        await Task.WhenAll(clients.Select(client => client.DisconnectAsync())).ConfigureAwait(false);
        await Task.WhenAll(clients.Select(async client => await client.DisposeAsync().ConfigureAwait(false))).ConfigureAwait(false);
    }

    private static Task RunAllAsync(IEnumerable<MqttClient> clients, Func<MqttClient, int, CancellationToken, Task> func, int maxDop, CancellationToken cancellationToken) =>
        Parallel.ForEachAsync(
            clients.Select((client, index) => (Client: client, Index: index)),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDop,
                TaskScheduler = TaskScheduler.Default
            },
            async (p, token) => await func(p.Client, p.Index, token).ConfigureAwait(false));
}