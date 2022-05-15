using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public delegate void MessageReceivedHandler(object sender, in MqttMessage message);

    private readonly ChannelReader<MqttMessage> incomingQueueReader;
    private readonly ChannelWriter<MqttMessage> incomingQueueWriter;
    private readonly ObserversContainer<MqttMessage> publishObservers;

    public ObserversContainer<MqttMessage>.Subscription SubscribeMessageObserver(IObserver<MqttMessage> observer) => publishObservers.Subscribe(observer);

    protected sealed override void OnPublish(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!PublishPacket.TryReadPayload(in reminder, header, (int)reminder.Length, out var id, out var topic, out var payload))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBLISH");
        }

        var qosLevel = (byte)((header >> 1) & QoSMask);
        var retain = (header & Retain) == Retain;

        switch (qosLevel)
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
        if (!SequenceExtensions.TryReadUInt16(in reminder, out var id))
        {
            MqttPacketHelpers.ThrowInvalidFormat("PUBREL");
        }

        sessionState.RemoveQoS2(id);

        Post(PubCompPacketMask | id);
    }

    private void DispatchMessage(string topic, ReadOnlyMemory<byte> payload, bool retained) => incomingQueueWriter.TryWrite(new(topic, payload, retained));

#pragma warning disable CA1031 // Do not catch general exception types - method should not throw by design
    private async Task StartMessageNotifierAsync(CancellationToken stoppingToken)
    {
        while (await incomingQueueReader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (incomingQueueReader.TryRead(out var message))
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    MessageReceived?.Invoke(this, in message);
                }
                catch
                {
                    //ignore
                }

                publishObservers.Notify(message);
            }
        }
    }
#pragma warning restore

#pragma warning disable CA1003 // Use generic event handler instances
    public event MessageReceivedHandler MessageReceived;
#pragma warning restore CA1003
}