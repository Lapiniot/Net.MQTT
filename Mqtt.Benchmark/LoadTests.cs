using System.Diagnostics;
using System.Net.Mqtt;
using System.Text;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static class LoadTests
{
    private const int MaxProgressWidth = 120;

    internal static async Task PublishConcurrentTestAsync(MqttClientBuilder clientBuilder, TestProfile profile)
    {
        var (_, numMessages, numClients, qosLevel, timeout, updateInterval, noProgress, maxConcurrent) = profile;
        int numConcurrent = maxConcurrent ?? numClients;

        using var cts = new CancellationTokenSource(timeout);
        var cancellationToken = cts.Token;

        var total = numClients * numMessages;
        var count = 0;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var payload = Encoding.UTF8.GetBytes(id);

        var clients = new List<MqttClient>();
        for(int i = 0; i < numClients; i++)
        {
            clients.Add(clientBuilder.BuildV4());
        }

        RenderTestSettings("publish", numClients, numMessages, qosLevel, numConcurrent);
        Console.ForegroundColor = ConsoleColor.DarkGray;

        await ConnectAllAsync(clients, cancellationToken).ConfigureAwait(false);

        var stopwatch = new Stopwatch();

        using var updateProgressCts = new CancellationTokenSource();
        try
        {
            if(!noProgress)
            {
                _ = UpdateProgressAsync(updateProgressCts.Token);
            }

            stopwatch.Start();
            await RunAllAsync(clients, async (client, index, token) =>
            {
                for(int i = 0; i < numMessages; i++)
                {
                    await client.PublishAsync($"TEST-{id}/CLIENT-{index:D6}/MSG-{i:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
                    lock(clients) count++;
                }
            }, numConcurrent, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            RenderReport(stopwatch.Elapsed);
        }
        finally
        {
            updateProgressCts.Cancel();
            stopwatch.Stop();
            await DisconnectAllAsync(clients).ConfigureAwait(false);
        }

        async Task UpdateProgressAsync(CancellationToken cancellationToken)
        {
            RenderProgress(0);
            using var timer = new PeriodicTimer(updateInterval);
            while(await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                RenderProgress(count / (double)total);
            }
        }
    }

    internal static async Task PublishReceiveConcurrentTestAsync(MqttClientBuilder clientBuilder, TestProfile profile)
    {
        var (_, numMessages, numClients, qosLevel, timeout, updateInterval, noProgress, maxConcurrent) = profile;
        int numConcurrent = maxConcurrent ?? numClients;
        using var cts = new CancellationTokenSource(timeout);
        var cancellationToken = cts.Token;

        int total = numClients * numMessages;
        using var evt = new CountdownEvent(total);

        var clients = new List<MqttClient>();
        for(int i = 0; i < numClients; i++)
        {
            clients.Add(clientBuilder.BuildV4());
        }

        void OnReceived(object sender, MessageReceivedEventArgs e) { evt.Signal(); }

        RenderTestSettings("publish/receive", numClients, numMessages, qosLevel, numConcurrent);
        Console.ForegroundColor = ConsoleColor.DarkGray;

        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var payload = Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));

        await ConnectAllAsync(clients, cancellationToken).ConfigureAwait(false);

        await RunAllAsync(clients, (client, index, token) =>
        {
            client.MessageReceived += OnReceived;
            return client.SubscribeAsync(new[] { ($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS0) }, token);
        }, numConcurrent, cancellationToken).ConfigureAwait(false);

        var stopwatch = new Stopwatch();

        using var updateProgressCts = new CancellationTokenSource();
        try
        {
            if(!noProgress)
            {
                _ = UpdateProgressAsync(updateProgressCts.Token);
            }

            stopwatch.Start();
            await RunAllAsync(clients, async (client, index, token) =>
            {
                for(int i = 0; i < numMessages; i++)
                {
                    await client.PublishAsync($"TEST-{id}/CLIENT-{index:D6}/MSG-{i:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
                }
            }, numConcurrent, cancellationToken).ConfigureAwait(false);

            evt.Wait(cancellationToken);

            stopwatch.Stop();
            RenderReport(stopwatch.Elapsed);
        }
        finally
        {
            updateProgressCts.Cancel();
            stopwatch.Stop();
            await RunAllAsync(clients, (client, index, token) =>
            {
                client.MessageReceived -= OnReceived;
                return client.DisconnectAsync();
            }, numConcurrent, cancellationToken).ConfigureAwait(false);
            await DisconnectAllAsync(clients).ConfigureAwait(false);
        }

        async Task UpdateProgressAsync(CancellationToken cancellationToken)
        {
            RenderProgress(0);
            using var timer = new PeriodicTimer(updateInterval);
            while(await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                RenderProgress(1 - evt.CurrentCount / (double)total);
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
        Console.WriteLine(new string('-', Math.Min(Console.WindowWidth, 70)));
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
}