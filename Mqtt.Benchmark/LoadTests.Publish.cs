using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

internal static partial class LoadTests
{
    internal static async Task PublishTestAsync(MqttClientBuilder clientBuilder, TestProfile profile)
    {
        var (_, numMessages, numClients, _, qosLevel, _, _, _, maxConcurrent, minPayloadSize, maxPayloadSize) = profile;
        var total = numClients * numMessages;
        var numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var count = 0;
        double GetCurrentProgress() { return count / (double)total; }

        RenderTestSettings("publish", numClients, numMessages, qosLevel, numConcurrent);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            testCore: async (client, index, token) =>
            {
                for(var i = 0; i < numMessages; i++)
                {
                    await PublishAsync(client, index, qosLevel, minPayloadSize, maxPayloadSize, id, i, token).ConfigureAwait(false);
                    Interlocked.Increment(ref count);
                }
            },
            GetCurrentProgress).ConfigureAwait(false);
    }
}