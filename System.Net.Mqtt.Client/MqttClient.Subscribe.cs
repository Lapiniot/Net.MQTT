using System.Buffers;
using System.Net.Mqtt.Packets;
using static System.String;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public virtual Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
    {
        return SendPacketAsync<byte[]>(id => new SubscribePacket(id, topics.Select(t => (t.topic, (byte)t.qos)).ToArray()), cancellationToken);
    }

    public virtual Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        return SendPacketAsync<object>(id => new UnsubscribePacket(id, topics), cancellationToken);
    }

    protected override void OnSubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!SubAckPacket.TryReadPayload(reminder, (int)reminder.Length, out var packet))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "SUBACK"));
        }

        AcknowledgePacket(packet.Id, packet.Result);
    }

    protected override void OnUnsubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadUInt16(in reminder, out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBACK"));
        }

        AcknowledgePacket(id);
    }
}