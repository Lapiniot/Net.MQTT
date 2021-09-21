using System.Buffers;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;

using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.QoSLevel;
using static System.String;

namespace System.Net.Mqtt.Client;

public partial class MqttClient : IObservable<MqttMessage>
{
    public virtual void Publish(string topic, Memory<byte> payload, QoSLevel qosLevel = AtMostOnce, bool retain = false)
    {
        var packet = CreatePublishPacket(topic, payload, qosLevel, retain);

        Post(packet);
    }

    public virtual Task PublishAsync(string topic, Memory<byte> payload, QoSLevel qosLevel = AtMostOnce,
        bool retain = false, CancellationToken cancellationToken = default)
    {
        var packet = CreatePublishPacket(topic, payload, qosLevel, retain);

        return SendAsync(packet, cancellationToken);
    }

    private PublishPacket CreatePublishPacket(string topic, Memory<byte> payload, QoSLevel qosLevel, bool retain)
    {
        return qosLevel is AtLeastOnce or ExactlyOnce
            ? sessionState.AddPublishToResend(topic, payload, (byte)qosLevel, retain)
            : new PublishPacket(0, 0, topic, payload, retain);
    }

    protected override void OnPubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!reminder.TryReadUInt16(out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBACK"));
        }

        sessionState.RemoveFromResend(id);
    }

    protected override void OnPubRec(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!reminder.TryReadUInt16(out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBREC"));
        }

        var pubRelPacket = sessionState.AddPubRelToResend(id);

        Post(pubRelPacket);
    }

    protected override void OnPubComp(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!reminder.TryReadUInt16(out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBCOMP"));
        }

        sessionState.RemoveFromResend(id);
    }
}