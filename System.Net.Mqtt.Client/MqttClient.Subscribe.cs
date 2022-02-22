using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public virtual Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default) =>
        SendPacketAsync<byte[]>(id => new SubscribePacket(id, topics.Select(t => (t.topic, (byte)t.qos)).ToArray()), cancellationToken);

    public virtual Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default) =>
        SendPacketAsync<object>(id => new UnsubscribePacket(id, topics), cancellationToken);

    protected sealed override void OnSubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SubAckPacket.TryReadPayload(in reminder, (int)reminder.Length, out var packet))
        {
            ThrowInvalidPacketFormat("SUBACK");
        }

        AcknowledgePacket(packet.Id, packet.Feedback);
    }

    protected sealed override void OnUnsubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("UNSUBACK");
        }

        AcknowledgePacket(id);
    }
}