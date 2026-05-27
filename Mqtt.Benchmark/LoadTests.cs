using System.Buffers;
using System.Diagnostics;
using Net.Mqtt.Client;

namespace Mqtt.Benchmark;

#pragma warning disable CA5394 // Random is an insecure random number generator. Use cryptographically secure random number generators when randomness is required for security.

internal sealed record TestSpec(
    Func<IEnumerable<MqttClient>, CancellationToken, Task> Action,
    Func<IEnumerable<MqttClient>, CancellationToken, Task>? Setup = null,
    Func<IEnumerable<MqttClient>, CancellationToken, Task>? Teardown = null);

internal static partial class LoadTests
{
    private const int MaxProgressWidth = 120;

    internal static async Task GenericTestAsync(MqttClientBuilder clientBuilder,
        TestSpec testSpec, ProfileOptions profile, Func<double> getProgressCallback,
        CancellationToken stoppingToken = default)
    {
        using var cts = new CancellationTokenSource(profile.TimeoutOverall);
        using var jointCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
        var cancellationToken = jointCts.Token;
        var benchmarkId = CorrelationIdGenerator.GetNext();

        var clients = new List<MqttClient>(profile.NumClients);
        for (var i = 0; i < profile.NumClients; i++)
        {
            clients.Add(clientBuilder.Build($"benchmark-{benchmarkId:x8}-{i:x8}"));
        }

        var (action, setup, teardown) = testSpec;

        TimeSpan elapsed;

        try
        {
            RenderStatus("Connecting clients...");

            await ConnectAllAsync(clients, cancellationToken);

            if (setup is not null)
            {
                await setup(clients, cancellationToken);
            }

            var startingTimestamp = Stopwatch.GetTimestamp();

            try
            {
                RenderStatus("");
                await using (new CancelAwaitableScope(profile.NoProgress
                    ? static _ => Task.CompletedTask
                    : UpdateProgressAsync))
                {
                    await action(clients, cancellationToken);
                }

                elapsed = Stopwatch.GetElapsedTime(startingTimestamp);
            }
            finally
            {
                RenderStatus("Finalizing...");
                if (teardown is not null)
                {
                    await teardown(clients, cancellationToken);
                }
            }
        }
        finally
        {
            RenderStatus("Disconnecting clients...");
            await DisconnectAllAsync(clients);
        }

        RenderReport(elapsed, profile.NumClients * profile.NumMessages);

        async Task UpdateProgressAsync(CancellationToken token)
        {
            RenderProgress(0);
            using var timer = new PeriodicTimer(profile.UpdateInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    RenderProgress(getProgressCallback());
                }
            }
            catch (OperationCanceledException) { }
        }

        static void RenderStatus(string text)
        {
            Console.Write("\e[2K\r\e[38;5;248m");
            Console.WriteLine(text);
            Console.WriteLine();
            Console.SetCursorPosition(0, Console.CursorTop - 2);
        }
    }

    private static async Task PublishAsync(MqttClient client, int clientIndex, QoSLevel qosLevel,
        int minPayloadSize, int maxPayloadSize, string testId, int messageIndex,
        CancellationToken cancellationToken)
    {
        var length = Random.Shared.Next(minPayloadSize, maxPayloadSize);
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var payload = new ReadOnlyMemory<byte>(buffer)[..length];
            await client.PublishAsync($"TEST-{testId}/CLIENT-{clientIndex:D6}/MSG-{messageIndex:D6}",
                payload, qosLevel, cancellationToken: cancellationToken);
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
        Console.WriteLine("\nElapsed time: {0:g} ({1:N2} ms.)", elapsed, elapsed.TotalMilliseconds);
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
            destination[width + 2] = ' ';
            progress.TryFormat(destination.Slice(width + 3), out _, format: "P2", null);
        });

        Console.Write("\e[38;5;105m");
        Console.Write(line);
        Console.Write("\e[39m\e[22m");
    }

    private static async Task ConnectAllAsync(IEnumerable<MqttClient> clients, CancellationToken cancellationToken)
    {
        await Parallel.ForEachAsync(clients, cancellationToken,
            body: static async (client, token) => await client.ConnectAsync(token));
    }

    private static async Task DisconnectAllAsync(IReadOnlyCollection<MqttClient> clients)
    {
        await Parallel.ForEachAsync(clients, CancellationToken.None,
            body: static async (client, _) =>
            {
                await using (client)
                {
                    await client.DisconnectAsync();
                }
            });
    }

    private static Task RunAllAsync(IEnumerable<MqttClient> clients,
        Func<MqttClient, int, CancellationToken, Task> action,
        int maxDop, CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(clients.Select((client, index) => (Client: client, Index: index)),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDop,
                TaskScheduler = TaskScheduler.Default
            },
            (pair, token) => new ValueTask(action(pair.Client, pair.Index, token)));
    }
}