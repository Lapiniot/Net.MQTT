using System.Net.Mqtt.Extensions;
using System.Threading.Channels;

namespace System.Net.Mqtt.Server;

public abstract class MqttServerSessionState : MqttSessionState
{
    protected MqttServerSessionState(string clientId, Channel<Message> outgoingChannelImpl,
        DateTime createdAt, int maxInFlight) : base(maxInFlight)
    {
        ArgumentNullException.ThrowIfNull(outgoingChannelImpl);

        ClientId = clientId;
        CreatedAt = createdAt;
        (OutgoingReader, OutgoingWriter) = outgoingChannelImpl;
    }

    public string ClientId { get; }
    public DateTime CreatedAt { get; }
    public bool IsActive { get; set; }
    public ChannelReader<Message> OutgoingReader { get; }
    public ChannelWriter<Message> OutgoingWriter { get; }

    #region Subscription state management

    public abstract bool TopicMatches(string topic, out byte maxQoS);

    public abstract byte[] Subscribe(IReadOnlyList<(string Filter, byte QoS)> filters);

    public abstract void Unsubscribe(IReadOnlyList<string> filters);

    #endregion
}