using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public readonly record struct PublishDeliveryState(byte Flags, ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, PublishPacketProperties Properties);

public sealed class MqttServerSessionState5 : MqttServerSessionState<Message5, PublishDeliveryState, MqttServerSessionSubscriptionState5>
{
    public MqttServerSessionState5(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, new MqttServerSessionSubscriptionState5(), Channel.CreateUnbounded<Message5>(), createdAt, maxInFlight)
    { }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out SubscriptionOptions? options, out IReadOnlyList<uint>? subscriptionIds) =>
        Subscriptions.TopicMatches(topic, out options, out subscriptionIds);

    public Task<ushort> CreateMessageDeliveryStateAsync(byte flags, ReadOnlyMemory<byte> topic,
        ReadOnlyMemory<byte> payload, PublishPacketProperties properties, CancellationToken cancellationToken)
        => CreateMessageDeliveryStateAsync(new((byte)(flags | PacketFlags.Duplicate), topic, payload, properties), cancellationToken);

    public new void DiscardMessageDeliveryState(ushort id) => base.DiscardMessageDeliveryState(id);
}