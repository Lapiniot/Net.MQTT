using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Client;

#pragma warning disable CA1003

public delegate void MessageReceivedHandler(object sender, in MqttMessage message);
public partial class MqttClient
{
    private readonly ChannelReader<MqttMessage> incomingQueueReader;
    private readonly ChannelWriter<MqttMessage> incomingQueueWriter;
    private readonly ObserversContainer<MqttMessage> publishObservers;

    public Subscription<MqttMessage> SubscribeMessageObserver(IObserver<MqttMessage> observer) => publishObservers.Subscribe(observer);

    protected sealed override void OnPublish(byte header, ReadOnlySequence<byte> reminder)
    {
        var qos = (header >> 1) & QoSMask;
        if (!PublishPacket.TryReadPayload(in reminder, qos != 0, (int)reminder.Length, out var id, out var topic, out var payload))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBLISH");
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
                MqttPacketHelpers.ThrowInvalidFormat("PUBLISH");
                break;
        }
    }

    protected sealed override void OnPubRel(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREL");
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

                try
                {
                    MessageReceived?.Invoke(this, message);
                }
#pragma warning disable CA1031
                catch
                {
                    //ignore
                }
#pragma warning restore CA1031

                publishObservers.Notify(message);
            }
        }
    }

    public event MessageReceivedHandler MessageReceived;
}