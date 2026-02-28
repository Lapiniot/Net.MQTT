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

        Task Setup(MqttClient client, int index, AsyncCountdownEvent _, CancellationToken token)
        {
            client.MessageReceived += OnReceived;
            return client.SubscribeAsync([($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS2)], token);
        }

        async Task Action(MqttClient client, int index, AsyncCountdownEvent acde, CancellationToken token)
        {
            for (var i = 0; i < profile.NumMessages; i++)
            {
                await PublishAsync(client, index, profile.QoSLevel,
                    profile.MinPayloadSize, profile.MaxPayloadSize, id, i, token)
                    .ConfigureAwait(false);
            }

            await client.WaitMessageDeliveryCompleteAsync(token).ConfigureAwait(false);
            await acde.WaitAsync(token).ConfigureAwait(false);
        }

        Task Teardown(MqttClient client, int index, AsyncCountdownEvent _, CancellationToken token)
        {
            client.MessageReceived -= OnReceived;
            return client.UnsubscribeAsync([$"TEST-{id}/CLIENT-{index:D6}/#"], token);
        }

        await GenericTestAsync(clientBuilder, new(Action, Setup, Teardown), profile, numConcurrent,
            GetCurrentProgress, state: countDownEvent, stoppingToken).ConfigureAwait(false);
    }
}