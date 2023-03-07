namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public virtual Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default) =>
        SendPacketAsync<byte[]>(id => new SubscribePacket(id, topics.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.qos)).ToArray()), cancellationToken);

    public virtual Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default) =>
        SendPacketAsync<object>(id => new UnsubscribePacket(id, topics.Select(t => (ReadOnlyMemory<byte>)UTF8.GetBytes(t)).ToArray()), cancellationToken);

    protected sealed override void OnSubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SubAckPacket.TryReadPayload(in reminder, (int)reminder.Length, out var packet))
        {
            MqttPacketHelpers.ThrowInvalidFormat("SUBACK");
        }

        AcknowledgePacket(packet.Id, packet.Feedback);
    }

    protected sealed override void OnUnsubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SE.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("UNSUBACK");
        }

        AcknowledgePacket(id);
    }
}