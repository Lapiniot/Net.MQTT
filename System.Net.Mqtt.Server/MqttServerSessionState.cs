﻿namespace System.Net.Mqtt.Server;

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

    public abstract bool TopicMatches(ReadOnlySpan<byte> topic, out byte maxQoS);

    public abstract byte[] Subscribe(IReadOnlyList<(byte[] Filter, byte QoS)> filters);

    public abstract void Unsubscribe(IReadOnlyList<byte[]> filters);

    #endregion
}