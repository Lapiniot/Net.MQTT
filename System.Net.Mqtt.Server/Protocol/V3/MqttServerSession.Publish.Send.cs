using System.Buffers;
using static System.String;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    private async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            var (topic, payload, qos, _) = await sessionState.DequeueMessageAsync(stoppingToken).ConfigureAwait(false);

            switch(qos)
            {
                case 0:
                    PostPublish(0, 0, topic, in payload);
                    break;

                case 1:
                case 2:
                    var flags = (byte)(qos << 1);
                    var id = sessionState.AddPublishToResend(flags, topic, in payload);
                    PostPublish(flags, id, topic, in payload);
                    break;

                default:
                    throw new InvalidDataException("Invalid QosLevel value");
            }
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
        PostRaw(PacketFlags.PubRelPacketMask | id);
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