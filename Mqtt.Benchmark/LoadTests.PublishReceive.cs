using System.Net.Mqtt;
using System.Text;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task PublishReceiveTestAsync(MqttClientBuilder clientBuilder, TestProfile profile)
    {
        var (_, numMessages, numClients, _, qosLevel, timeout, updateInterval, noProgress, maxConcurrent) = profile;
        var total = numClients * numMessages;
        int numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var payload = Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));
        using var evt = new CountdownEvent(total);
        void OnReceived(object sender, MessageReceivedEventArgs e) { evt.Signal(); }
        double GetCurrentProgress() { return 1 - ((double)evt.CurrentCount) / total; }

        RenderTestSettings("publish/receive", numClients, numMessages, qosLevel, numConcurrent);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            testCore: async (client, index, token) =>
            {
                for(int i = 0; i < numMessages; i++)
                {
                    await client.PublishAsync($"TEST-{id}/CLIENT-{index:D6}/MSG-{i:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
                }
            },
            GetCurrentProgress,
            setupClient: (client, index, token) =>
            {
                client.MessageReceived += OnReceived;
                return client.SubscribeAsync(new[] { ($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS0) }, token);
            },
            cleanupClient: (client, index, token) =>
            {
                client.MessageReceived -= OnReceived;
                return Task.CompletedTask;
            },
            finalizeTest: (token) =>
            {
                evt.Wait(token);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
    }
}