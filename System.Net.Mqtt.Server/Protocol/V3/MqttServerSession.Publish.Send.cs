using System.Buffers;
using System.Net.Mqtt.Packets;
using static System.String;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    private async Task ProcessMessageAsync(CancellationToken cancellationToken)
    {
        var (topic, payload, qoSLevel, _) = await sessionState.DequeueAsync(cancellationToken).ConfigureAwait(false);

        switch(qoSLevel)
        {
            case 0:
                Post(new PublishPacket(0, default, topic, payload));
                break;

            case 1:
            case 2:
                Post(sessionState.AddPublishToResend(topic, payload, qoSLevel));
                break;

            default:
                throw new InvalidDataException("Invalid QosLevel value");
        }
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