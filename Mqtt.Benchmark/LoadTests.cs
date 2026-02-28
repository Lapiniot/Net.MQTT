using System.Buffers;
using System.Diagnostics;
using Net.Mqtt.Client;

namespace Mqtt.Benchmark;

#pragma warning disable CA5394 // Random is an insecure random number generator. Use cryptographically secure random number generators when randomness is required for security.

internal sealed record TestSpec<T>(
    Func<MqttClient, int, T, CancellationToken, Task> Action,
    Func<MqttClient, int, T, CancellationToken, Task>? Setup = null,
    Func<MqttClient, int, T, CancellationToken, Task>? Teardown = null);

internal static partial class LoadTests
{
    private const int MaxProgressWidth = 120;

    internal static async Task GenericTestAsync<T>(MqttClientBuilder clientBuilder,
        TestSpec<T> testSpec, ProfileOptions profile, int numConcurrent, Func<double> getProgressCallback,
        T state, CancellationToken stoppingToken = default)
    {
        using var cts = new CancellationTokenSource(profile.TimeoutOverall);
        using var jointCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
        var cancellationToken = jointCts.Token;

        var clients = new List<MqttClient>(profile.NumClients);
        for (var i = 0; i < profile.NumClients; i++)
        {
            clients.Add(clientBuilder.Build());
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;

        var (action, setup, teardown) = testSpec;

        try
        {
            await ConnectAllAsync(clients, cancellationToken).ConfigureAwait(false);

            if (setup is not null)
            {
                await RunAllAsync(clients, setup, numConcurrent, state, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var startingTimestamp = Stopwatch.GetTimestamp();
                await using (new CancelAwaitableScope(profile.NoProgress
                    ? static _ => Task.CompletedTask
                    : UpdateProgressAsync))
                {
                    await RunAllAsync(clients, action, numConcurrent, state, cancellationToken).ConfigureAwait(false);
                }

                RenderReport(Stopwatch.GetElapsedTime(startingTimestamp), profile.NumClients * profile.NumMessages);
            }
            finally
            {
                if (teardown is not null)
                {
                    await RunAllAsync(clients, teardown, numConcurrent, state, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            await DisconnectAllAsync(clients).ConfigureAwait(false);
        }

        async Task UpdateProgressAsync(CancellationToken token)
        {
            RenderProgress(0);
            using var timer = new PeriodicTimer(profile.UpdateInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                {
                    RenderProgress(getProgressCallback());
                }
            }
            catch (OperationCanceledException) { }
        }
    }

    private static async Task PublishAsync(MqttClient client, int clientIndex, QoSLevel qosLevel, int minPayloadSize, int maxPayloadSize, string testId, int messageIndex, CancellationToken token)
    {
        var length = Random.Shared.Next(minPayloadSize, maxPayloadSize);
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

    private static void RenderTestSettings(string testName, Uri server, int numClients, int numMessages, QoSLevel qosLevel, int maxConcurrent, int version) =>
        Console.WriteLine($"""
        Running '{testName}' test...
        
        Connection:             {server}
        MQTT protocol level:    {version}
        Connected clients:      {numClients}
        Concurrent clients:     {maxConcurrent}
        Messages per client:    {numMessages}
        QoS level:              {qosLevel}
        """);

    private static void RenderReport(TimeSpan elapsed, int totalIterations)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"{{0,-{Console.WindowWidth}}}", new string('-', Math.Min(Console.WindowWidth, 70)));
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("Elapsed time: {0:hh\\:mm\\:ss\\.fff} ({1:N2} ms.)", elapsed, elapsed.TotalMilliseconds);
        Console.WriteLine("Avg. rate:    {0:N2} iteration/sec.\n", totalIterations / elapsed.TotalSeconds);
    }

    private static void RenderProgress(double progress)
    {
        Console.CursorLeft = 0;
        var maxWidth = Math.Min(MaxProgressWidth, Console.WindowWidth);

        var line = string.Create(maxWidth, progress, static (destination, progress) =>
        {
            var width = destination.Length - 12;
            var bars = (int)(width * progress);
            destination[0] = '│';
            destination.Slice(1, bars).Fill('█');
            destination.Slice(1 + bars, width - bars).Fill('·');
            destination[width + 1] = '│';
            progress.TryFormat(destination.Slice(width + 2), out _, format: " 0.00%");
        });

        Console.Write("\e[38;5;105m");
        Console.Write(line);
        Console.Write("\e[39m\e[22m");
    }

    private static async Task ConnectAllAsync(IEnumerable<MqttClient> clients, CancellationToken cancellationToken)
    {
        Console.WriteLine("Connecting clients...");
        Console.WriteLine();
        Console.SetCursorPosition(0, Console.CursorTop - 2);

        await Parallel.ForEachAsync(clients, cancellationToken, body: static async (client, token) =>
            await client.ConnectAsync(token).ConfigureAwait(false)).ConfigureAwait(false);
    }

    private static async Task DisconnectAllAsync(IReadOnlyCollection<MqttClient> clients) =>
        await Task.WhenAll(clients.Select(static async client =>
        {
            await using (client.ConfigureAwait(false))
            {
                await client.DisconnectAsync().ConfigureAwait(false);
            }
        })).ConfigureAwait(false);

    private static Task RunAllAsync<T>(IEnumerable<MqttClient> clients,
        Func<MqttClient, int, T, CancellationToken, Task> action,
        int maxDop, T state, CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(clients.Select((client, index) => (Client: client, Index: index)),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDop,
                TaskScheduler = TaskScheduler.Default
            },
            async (p, token) => await action(p.Client, p.Index, state, token).ConfigureAwait(false));
    }
}