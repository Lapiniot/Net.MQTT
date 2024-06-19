using System.Text;
using Net.Mqtt.Client;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task SubscribePublishReceiveTestAsync(Uri server, MqttClientBuilder clientBuilder, ProfileOptions profile, CancellationToken stoppingToken)
    {
        var total = profile.NumClients * profile.NumMessages;
        var numConcurrent = profile.MaxConcurrent ?? profile.NumClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));
        var evt = new AsyncCountdownEvent(total);

        void OnReceived(object sender, MqttMessageArgs<MqttMessage> _) => evt.Signal();

        double GetCurrentProgress() => 1 - (double)evt.CurrentCount / total;

        RenderTestSettings("subscribe/publish/receive", server, profile.NumClients, profile.NumMessages, profile.QoSLevel, numConcurrent, clientBuilder.Version);
        Console.WriteLine("Extra subscriptions:    {0}", profile.NumSubscriptions);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            async (client, index, token) =>
            {
                for (var i = 0; i < profile.NumMessages; i++)
                {
                    await PublishAsync(client, index, profile.QoSLevel, profile.MinPayloadSize, profile.MaxPayloadSize, id, i, token).ConfigureAwait(false);
                }

                await client.WaitMessageDeliveryCompleteAsync(token).ConfigureAwait(false);
            },
            GetCurrentProgress, async (client, index, token) =>
            {
                client.MessageReceived += OnReceived;

                var filters = new (string topic, QoSLevel qos)[profile.NumSubscriptions + 1];
                filters[^1] = ($"TEST-{id}/CLIENT-{index:D6}/#", QoSLevel.QoS2);
                for (var i = 0; i < filters.Length - 1; i++)
                {
                    filters[i] = ($"TEST-{id}/CLIENT-{index:D6}/EXTRA-{i:D3}", QoSLevel.QoS2);
                }

                await client.SubscribeAsync(filters, token).ConfigureAwait(false);
            }, async (client, index, token) =>
            {
                client.MessageReceived -= OnReceived;

                var filters = new string[profile.NumSubscriptions + 1];
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