using System.Net.Mqtt;
using System.Text;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task SubscibePublishReceiveTestAsync(MqttClientBuilder clientBuilder, TestProfile profile)
    {
        var (_, numMessages, numClients, numSubscriptions, qosLevel, timeout, updateInterval, noProgress, maxConcurrent) = profile;
        var total = numClients * numMessages;
        var numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var payload = Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));
        using var evt = new CountdownEvent(total);
        void OnReceived(object sender, MessageReceivedEventArgs e) { evt.Signal(); }
        double GetCurrentProgress() { return 1 - ((double)evt.CurrentCount) / total; }

        RenderTestSettings("subscribe/publish/receive", numClients, numMessages, qosLevel, numConcurrent);
        Console.WriteLine("Extra subscriptions:        {0}", numSubscriptions);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            testCore: async (client, index, token) =>
            {
                var filters = new (string topic, QoSLevel qos)[20];
                for(int i = 0; i < numSubscriptions; i++)
                {
                    filters[i] = ($"TEST-{id}/CLIENT-{index:D6}/EXTRA-{i:D3}", QoSLevel.QoS2);
                }

                await client.SubscribeAsync(filters, token).ConfigureAwait(false);

                for(int i = 0; i < numMessages; i++)
                {
                    await client.PublishAsync($"TEST-{id}/CLIENT-{index:D6}/MSG-{i:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
                }
            },
            GetCurrentProgress,
            setupClient: (client, index, token) =>
            {
                client.MessageReceived += OnReceived;
                return client.SubscribeAsync(new[] { ($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS2) }, token);
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