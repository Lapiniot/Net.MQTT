using System.Net.Mqtt.Packets.V3;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private readonly ChannelReader<MqttMessage> incomingQueueReader;
    private readonly ChannelWriter<MqttMessage> incomingQueueWriter;

    private void OnPublish(byte header, in ReadOnlySequence<byte> reminder)
    {
        var qos = (header >>> 1) & QoSMask;
        if (!PublishPacket.TryReadPayload(in reminder, qos != 0, (int)reminder.Length, out var id, out var topic, out var payload))
        {
            MalformedPacketException.Throw("PUBLISH");
        }

        var retain = (header & Retain) == Retain;

        switch (qos)
        {
            case 0:
                DispatchMessage(UTF8.GetString(topic), payload, retain);
                break;

            case 1:
                DispatchMessage(UTF8.GetString(topic), payload, retain);
                Post(PubAckPacketMask | id);
                break;

            case 2:
                if (sessionState.TryAddQoS2(id))
                {
                    DispatchMessage(UTF8.GetString(topic), payload, retain);
                }

                Post(PubRecPacketMask | id);
                break;

            default:
                MalformedPacketException.Throw("PUBLISH");
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPubRel(in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("PUBREL");
        }

        sessionState.RemoveQoS2(id);

        Post(PubCompPacketMask | id);
    }

    private void DispatchMessage(string topic, ReadOnlyMemory<byte> payload, bool retained) => incomingQueueWriter.TryWrite(new(topic, payload, retained));

    private async Task StartMessageNotifierAsync(CancellationToken stoppingToken)
    {
        while (await incomingQueueReader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (incomingQueueReader.TryRead(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();
                OnMessageReceived(message);
            }
        }
    }
}