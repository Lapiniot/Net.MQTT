using System.Buffers;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    private async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var (topic, payload, qos, _) = await sessionState.DequeueMessageAsync(stoppingToken).ConfigureAwait(false);

            switch (qos)
            {
                case 0:
                    PostPublish(0, 0, topic, in payload);
                    break;

                case 1:
                case 2:
                    await inflightSentinel.WaitAsync(stoppingToken).ConfigureAwait(false);
                    var flags = (byte)(qos << 1);
                    var id = sessionState.AddPublishToResend(flags, topic, in payload);
                    PostPublish(flags, id, topic, in payload);
                    break;

                default:
                    throw new InvalidDataException("Invalid QosLevel value");
            }
        }
    }

    protected sealed override void OnPubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBACK");
        }

        ReleaseInflightSlot(id);
    }

    protected sealed override void OnPubRec(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBREC");
        }

        sessionState.AddPubRelToResend(id);
        Post(PacketFlags.PubRelPacketMask | id);
    }

    protected sealed override void OnPubComp(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBCOMP");
        }

        ReleaseInflightSlot(id);
    }

    private void ReleaseInflightSlot(ushort id)
    {
        sessionState.RemoveFromResend(id);
        inflightSentinel.Release();
    }
}