using Net.Mqtt.Client;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task PublishTestAsync(Uri server, MqttClientBuilder clientBuilder, ProfileOptions profile, CancellationToken stoppingToken)
    {
        var total = profile.NumClients * profile.NumMessages;
        var numConcurrent = profile.MaxConcurrent ?? profile.NumClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var count = 0;

        double GetCurrentProgress() => count / (double)total;

        RenderTestSettings("publish", server, profile.NumClients, profile.NumMessages, profile.QoSLevel, numConcurrent, clientBuilder.Version);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            async (client, index, token) =>
            {
                for (var i = 0; i < profile.NumMessages; i++)
                {
                    await PublishAsync(client, index, profile.QoSLevel, profile.MinPayloadSize, profile.MaxPayloadSize, id, i, token).ConfigureAwait(false);
                    Interlocked.Increment(ref count);
                }

                await client.WaitMessageDeliveryCompleteAsync(token).ConfigureAwait(false);
            },
            GetCurrentProgress, stoppingToken: stoppingToken).ConfigureAwait(false);
    }
}