namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task PublishReceiveTestAsync(Uri server, MqttClientBuilder clientBuilder, TestProfile profile, CancellationToken stoppingToken)
    {
        var (_, numMessages, numClients, _, qosLevel, _, _, _, maxConcurrent, minPayloadSize, maxPayloadSize) = profile;
        var total = numClients * numMessages;
        var numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var evt = new AsyncCountdownEvent(total);

        void OnReceived(object sender, in MqttMessage _) => evt.Signal();

        double GetCurrentProgress() => 1 - (double)evt.CurrentCount / total;

        RenderTestSettings("publish/receive", server, numClients, numMessages, qosLevel, numConcurrent);
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
            GetCurrentProgress,
            (client, index, token) =>
            {
                client.MessageReceived += OnReceived;
                return client.SubscribeAsync(new[] { ($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS2) }, token);
            },
            (client, index, token) =>
            {
                client.MessageReceived -= OnReceived;
                return client.UnsubscribeAsync([$"TEST-{id}/CLIENT-{index:D6}/#"], token);
            },
            token => evt.WaitAsync(token),
            stoppingToken).ConfigureAwait(false);
    }
}