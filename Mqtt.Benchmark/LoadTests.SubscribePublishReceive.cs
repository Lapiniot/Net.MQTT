using System.Net.Mqtt;
using System.Text;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task SubscribePublishReceiveTestAsync(MqttClientBuilder clientBuilder, TestProfile profile, CancellationToken stoppingToken)
    {
        var (_, numMessages, numClients, numSubscriptions, qosLevel, _, _, _, maxConcurrent, minPayloadSize, maxPayloadSize) = profile;
        var total = numClients * numMessages;
        var numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));
        var evt = new AsyncCountdownEvent(total);

        void OnReceived(object sender, in MqttMessage _) => evt.Signal();

        double GetCurrentProgress() => 1 - (double)evt.CurrentCount / total;

        RenderTestSettings("subscribe/publish/receive", numClients, numMessages, qosLevel, numConcurrent);
        Console.WriteLine("Extra subscriptions:        {0}", numSubscriptions);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            async (client, index, token) =>
            {
                for (var i = 0; i < numMessages; i++)
                {
                    await PublishAsync(client, index, qosLevel, minPayloadSize, maxPayloadSize, id, i, token).ConfigureAwait(false);
                }

                await client.CompleteAsync().WaitAsync(token).ConfigureAwait(false);
            },
            GetCurrentProgress, async (client, index, token) =>
            {
                client.MessageReceived += OnReceived;

                var filters = new (string topic, QoSLevel qos)[numSubscriptions + 1];
                filters[^1] = ($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS2);
                for (var i = 0; i < filters.Length - 1; i++)
                {
                    filters[i] = ($"TEST-{id}/CLIENT-{index:D6}/EXTRA-{i:D3}", QoSLevel.QoS2);
                }

                await client.SubscribeAsync(filters, token).ConfigureAwait(false);
            }, async (client, index, token) =>
            {
                client.MessageReceived -= OnReceived;

                var filters = new string[numSubscriptions + 1];
                filters[^1] = $"TEST-{id}/CLIENT-{index:D6}/#";
                for (var i = 0; i < filters.Length - 1; i++)
                {
                    filters[i] = $"TEST-{id}/CLIENT-{index:D6}/EXTRA-{i:D3}";
                }

                await client.UnsubscribeAsync(filters, token).ConfigureAwait(false);
            },
            token => evt.WaitAsync(token),
            stoppingToken).ConfigureAwait(false);
    }
}