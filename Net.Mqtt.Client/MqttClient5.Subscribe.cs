using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public sealed partial class MqttClient5
{
    public override Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] filters,
        CancellationToken cancellationToken = default) => SubscribeAsync(
            filters.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.qos)).ToArray(),
            null, cancellationToken);

    public Task<byte[]> SubscribeAsync((string topic, SubscribeOptions options)[] filters,
        uint? subscriptionId = null, CancellationToken cancellationToken = default) => SubscribeAsync(
            filters.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.options.Flags)).ToArray(),
            subscriptionId, cancellationToken);

    private async Task<byte[]> SubscribeAsync((ReadOnlyMemory<byte>, byte)[] filters,
        uint? subscriptionId, CancellationToken cancellationToken)
    {
        if (subscriptionId is { } id)
        {
            ArgumentOutOfRangeException.ThrowIfZero(id);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(id, 268435455u);
        }

        if (!ConnectionAcknowledged)
        {
            await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);
        }

        var acknowledgeTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var packetId = sessionState!.RentId();
        pendingCompletions.TryAdd(packetId, acknowledgeTcs);

        try
        {
            Post(new SubscribePacket(packetId, filters) { SubscriptionIdentifier = subscriptionId });
            return (byte[])(await acknowledgeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false))!;
        }
        catch (OperationCanceledException)
        {
            acknowledgeTcs.TrySetCanceled(cancellationToken);
            throw;
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out _);
            sessionState.ReturnId(packetId);
        }
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        if (!ConnectionAcknowledged)
        {
            await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);
        }

        var acknowledgeTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var packetId = sessionState!.RentId();
        pendingCompletions.TryAdd(packetId, acknowledgeTcs);

        try
        {
            Post(new UnsubscribePacket(packetId, [.. topics.Select(t => (ReadOnlyMemory<byte>)UTF8.GetBytes(t))]));
            await acknowledgeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            acknowledgeTcs.TrySetCanceled(cancellationToken);
            throw;
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out _);
            sessionState.ReturnId(packetId);
        }
    }
}