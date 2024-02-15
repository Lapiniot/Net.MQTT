using Net.Mqtt.Client;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task PublishTestAsync(Uri server, MqttClientBuilder clientBuilder, TestProfile profile, CancellationToken stoppingToken)
    {
        var (_, cancellationToken, numClients, _, qosLevel, _, _, _, maxConcurrent, minPayloadSize, maxPayloadSize) = profile;
        var total = numClients * cancellationToken;
        var numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var count = 0;

        double GetCurrentProgress() => count / (double)total;

        RenderTestSettings("publish", server, numClients, cancellationToken, qosLevel, numConcurrent, clientBuilder.Version);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            async (client, index, token) =>
            {
                for (var i = 0; i < cancellationToken; i++)
                {
                    await PublishAsync(client, index, qosLevel, minPayloadSize, maxPayloadSize, id, i, token).ConfigureAwait(false);
                    Interlocked.Increment(ref count);
                }

                await client.WaitMessageDeliveryCompleteAsync(token).ConfigureAwait(false);
            },
            GetCurrentProgress, stoppingToken: stoppingToken).ConfigureAwait(false);
    }
}