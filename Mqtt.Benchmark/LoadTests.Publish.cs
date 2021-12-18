using System.Text;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;
internal static partial class LoadTests
{
    internal static async Task PublishTestAsync(MqttClientBuilder clientBuilder, TestProfile profile)
    {
        var (_, numMessages, numClients, _, qosLevel, timeout, updateInterval, noProgress, maxConcurrent) = profile;
        var total = numClients * numMessages;
        int numConcurrent = maxConcurrent ?? numClients;
        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var payload = Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));
        var count = 0;
        double GetCurrentProgress() { return count / (double)total; }

        RenderTestSettings("publish", numClients, numMessages, qosLevel, numConcurrent);
        Console.WriteLine();
        Console.WriteLine();

        await GenericTestAsync(clientBuilder, profile, numConcurrent,
            testCore: async (client, index, token) =>
            {
                for(int i = 0; i < numMessages; i++)
                {
                    await client.PublishAsync($"TEST-{id}/CLIENT-{index:D6}/MSG-{i:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
                    Interlocked.Increment(ref count);
                }
            },
            GetCurrentProgress,
            setupClient: DoNothing,
            cleanupClient: DoNothing,
            finalizeTest: _ => Task.CompletedTask).ConfigureAwait(false);
    }
}
