using System.Buffers;
using static System.String;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public virtual void Publish(string topic, in ReadOnlyMemory<byte> payload, QoSLevel qosLevel = AtMostOnce, bool retain = false)
    {
        var qos = (byte)qosLevel;
        var flags = (byte)(retain ? PacketFlags.Retain : 0);
        if(qos is 1 or 2)
        {
            flags |= (byte)(qos << 1);
            var id = sessionState.AddPublishToResend(flags, topic, in payload);
            PostPublish(flags, id, topic, in payload);
        }
        else
        {
            PostPublish(flags, 0, topic, in payload);
        }
    }

    public virtual Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, QoSLevel qosLevel = AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        var qos = (byte)qosLevel;
        var flags = (byte)(retain ? PacketFlags.Retain : 0);

        if(qos is not (1 or 2))
        {
            return SendPublishAsync(flags, 0, topic, payload, cancellationToken);
        }

        flags |= (byte)(qos << 1);
        var id = sessionState.AddPublishToResend(flags, topic, in payload);
        return SendPublishAsync(flags, id, topic, payload, cancellationToken);
    }

    protected override void OnPubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadUInt16(in reminder, out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBACK"));
        }

        sessionState.RemoveFromResend(id);
    }

    protected override void OnPubRec(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadUInt16(in reminder, out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBREC"));
        }

        sessionState.AddPubRelToResend(id);

        Post(PacketFlags.PubRelPacketMask | id);
    }

    protected override void OnPubComp(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadUInt16(in reminder, out var id))
        {
            throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PUBCOMP"));
        }

        sessionState.RemoveFromResend(id);
    }
}