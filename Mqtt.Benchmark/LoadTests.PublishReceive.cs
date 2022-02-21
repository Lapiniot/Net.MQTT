using System.Net.Mqtt;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task PublishReceiveTestAsync(MqttClientBuilder clientBuilder, TestProfile profile)
    {
        var (_, numMessages, numClients, _, qosLevel, _, _, _, maxConcurrent, minPayloadSize, maxPayloadSize) = profile;
        var total = numClients * numMessages;
        var numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        using var evt = new CountdownEvent(total);

        void OnReceived(object sender, in MqttMessage _) => evt.Signal();

        double GetCurrentProgress() => 1 - (double)evt.CurrentCount / total;

        RenderTestSettings("publish/receive", numClients, numMessages, qosLevel, numConcurrent);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            async (client, index, token) =>
            {
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
            (client, _, _) =>
            {
                client.MessageReceived -= OnReceived;
                return Task.CompletedTask;
            },
            token =>
            {
                evt.Wait(token);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
    }
}