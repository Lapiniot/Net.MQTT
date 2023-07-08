
namespace System.Net.Mqtt.Server.Protocol.V3;

public class MqttServerSessionState3 : MqttServerSessionState<Message3, PublishDeliveryState, MqttServerSessionSubscriptionState3>
{
    public Message3? WillMessage { get; set; }

    public MqttServerSessionState3(string clientId, DateTime createdAt) :
        this(clientId, new MqttServerSessionSubscriptionState3(), Channel.CreateUnbounded<Message3>(), createdAt)
    { }

    protected MqttServerSessionState3(string clientId, MqttServerSessionSubscriptionState3 subscriptions,
        Channel<Message3> outgoingChannelImpl, DateTime createdAt) :
        base(clientId, subscriptions, outgoingChannelImpl, createdAt)
    { }

    public bool TopicMatches(ReadOnlySpan<byte> topic, out byte maxQoS) => Subscriptions.TopicMatches(topic, out maxQoS);

    public sealed override void Trim()
    {
        Subscriptions.Trim();
        base.Trim();
    }

    public ushort CreateMessageDeliveryState(byte flags, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload) =>
        CreateDeliveryStateCore(new((byte)(flags | PacketFlags.Duplicate), topic, payload));

    public bool DiscardMessageDeliveryState(ushort packetId) => DiscardDeliveryStateCore(packetId);
}