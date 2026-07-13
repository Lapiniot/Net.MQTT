using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public sealed partial class MqttClient5
{
    public override Task<ReadOnlyMemory<byte>> SubscribeAsync((string topic, QoSLevel qos)[] filters,
        CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(filters.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.qos)).ToArray(),
            subscriptionId: null, cancellationToken);
    }

    public Task<ReadOnlyMemory<byte>> SubscribeAsync((string topic, SubscribeOptions options)[] filters,
        uint? subscriptionId = null, CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(filters.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.options.Flags)).ToArray(),
            subscriptionId, cancellationToken);
    }

    private async Task<ReadOnlyMemory<byte>> SubscribeAsync((ReadOnlyMemory<byte>, byte)[] filters,
        uint? subscriptionId, CancellationToken cancellationToken)
    {
        if (subscriptionId is { } id)
        {
            ArgumentOutOfRangeException.ThrowIfZero(id);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(id, 268435455u);
        }

        if (!ConnectionAcknowledged)
        {
            await WaitConnectionAcknowledgedAsync(cancellationToken).ConfigureAwait(false);
        }

        var packetId = sessionState!.RentId();

        try
        {
            using var cookie = AcquirePacketAcknowledgementCookie(packetId);
            Post(new SubscribePacket(packetId, filters) { SubscriptionIdentifier = subscriptionId });
            return await cookie.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sessionState.ReturnId(packetId);
        }
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        if (!ConnectionAcknowledged)
        {
            await WaitConnectionAcknowledgedAsync(cancellationToken).ConfigureAwait(false);
        }

        var packetId = sessionState!.RentId();

        try
        {
            using var cookie = AcquirePacketAcknowledgementCookie(packetId);
            Post(new UnsubscribePacket(packetId, [.. topics.Select(t => (ReadOnlyMemory<byte>)UTF8.GetBytes(t))]));
            await cookie.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sessionState.ReturnId(packetId);
        }
    }
}