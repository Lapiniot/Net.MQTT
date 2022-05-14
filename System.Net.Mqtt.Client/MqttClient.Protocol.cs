using System.Collections.Concurrent;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;

    private async Task<T> SendPacketAsync<T>(Func<ushort, MqttPacketWithId> packetFactory, CancellationToken cancellationToken) where T : class
    {
        var deliveryTcs = new TaskCompletionSource(RunContinuationsAsynchronously);
        var acknowledgeTcs = new TaskCompletionSource<object>(RunContinuationsAsynchronously);
        var packetId = sessionState.RentId();
        pendingCompletions.TryAdd(packetId, acknowledgeTcs);

        try
        {
            Post(packetFactory(packetId), deliveryTcs);
            await deliveryTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return await acknowledgeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as T;
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out _);
            sessionState.ReturnId(packetId);
        }
    }

    private void AcknowledgePacket(ushort packetId, object result = null)
    {
        if (pendingCompletions.TryGetValue(packetId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }
}