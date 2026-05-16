using Net.Mqtt.Client;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task PublishReceiveTestAsync(Uri server, MqttClientBuilder clientBuilder, ProfileOptions profile, CancellationToken stoppingToken)
    {
        var total = profile.NumClients * profile.NumMessages;
        var numConcurrent = profile.MaxConcurrent ?? profile.NumClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var countDownEvent = new AsyncCountdownEvent(total);

        void OnReceived(object sender, MqttMessageArgs<MqttMessage> _) => countDownEvent.Signal();

        double GetCurrentProgress() => 1 - (double)countDownEvent.CurrentCount / total;

        RenderTestSettings("publish/receive", server, profile.NumClients, profile.NumMessages, profile.QoSLevel, numConcurrent, clientBuilder.Version);
        Console.WriteLine();
        Console.WriteLine();

        Task Setup(IEnumerable<MqttClient> clients, CancellationToken token)
        {
            foreach (var client in clients)
            {
                client.MessageReceived += OnReceived;
            }

            return RunAllAsync(clients, (client, index, token) =>
            {
                return client.SubscribeAsync([($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS2)], token);
            }, numConcurrent, token);
        }

        async Task Action(IEnumerable<MqttClient> clients, CancellationToken token)
        {
            await RunAllAsync(clients, async (client, index, token) =>
            {
                for (var i = 0; i < profile.NumMessages; i++)
                {
                    await PublishAsync(client, index, profile.QoSLevel,
                        profile.MinPayloadSize, profile.MaxPayloadSize, id, i, token);
                }

                await client.WaitMessageDeliveryCompleteAsync(token);
            }, numConcurrent, token);

            await countDownEvent.WaitAsync(token);
        }

        Task Teardown(IEnumerable<MqttClient> clients, CancellationToken token)
        {
            foreach (var client in clients)
            {
                client.MessageReceived -= OnReceived;
            }

            return Task.CompletedTask;
        }

        await GenericTestAsync(clientBuilder, new(Action, Setup, Teardown), profile, GetCurrentProgress, stoppingToken);
    }
}