using System.Collections.Concurrent;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;

    private async Task<T> SendPacketAsync<T>(Func<ushort, MqttPacketWithId> packetFactory, CancellationToken cancellationToken) where T : class
    {
        var packetId = sessionState.RentId();
        var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingCompletions.TryAdd(packetId, completionSource);

        try
        {
            await SendAsync(packetFactory(packetId), cancellationToken).ConfigureAwait(false);
            return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as T;
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out _);
            sessionState.ReturnId(packetId);
        }
    }

    private void AcknowledgePacket(ushort packetId, object result = null)
    {
        if(pendingCompletions.TryGetValue(packetId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }
}