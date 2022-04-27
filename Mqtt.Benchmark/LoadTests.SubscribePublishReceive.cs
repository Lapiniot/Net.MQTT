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
        using var evt = new CountdownEvent(total);

        void OnReceived(object sender, in MqttMessage _) => evt.Signal();

        double GetCurrentProgress() => 1 - (double)evt.CurrentCount / total;

        RenderTestSettings("subscribe/publish/receive", numClients, numMessages, qosLevel, numConcurrent);
        Console.WriteLine("Extra subscriptions:        {0}", numSubscriptions);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            async (client, index, token) =>
            {
                var filters = new (string topic, QoSLevel qos)[20];
                for (var i = 0; i < numSubscriptions; i++)
                {
                    filters[i] = ($"TEST-{id}/CLIENT-{index:D6}/EXTRA-{i:D3}", QoSLevel.QoS2);
                }

                await client.SubscribeAsync(filters, token).ConfigureAwait(false);

                for (var i = 0; i < numMessages; i++)
                {
                    await PublishAsync(client, index, qosLevel, minPayloadSize, maxPayloadSize, id, i, token).ConfigureAwait(false);
                }
            },
            GetCurrentProgress,
            (client, index, token) =>
            {
                client.MessageReceived += OnReceived;
                return client.SubscribeAsync(new[] { ($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS2) }, token);
            },
            (client, index, token) =>
            {
                client.MessageReceived -= OnReceived;
                return client.UnsubscribeAsync(new[] { $"TEST-{id}/CLIENT-{index:D6}/#" }, token);
            },
            token =>
            {
                evt.Wait(token);
                return Task.CompletedTask;
            },
            stoppingToken).ConfigureAwait(false);
    }
}