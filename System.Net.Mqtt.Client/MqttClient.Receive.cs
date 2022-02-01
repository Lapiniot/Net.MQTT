using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Threading.Channels;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    public delegate void MessageReceivedHandler(object sender, in MqttMessage message);
    private readonly ChannelReader<MqttMessage> incomingQueueReader;
    private readonly ChannelWriter<MqttMessage> incomingQueueWriter;
#pragma warning disable CA2213 // False positive from roslyn analyzer
    private readonly WorkerLoop dispatcher;
#pragma warning restore CA2213
    private readonly ObserversContainer<MqttMessage> publishObservers;

    public IDisposable Subscribe(IObserver<MqttMessage> observer)
    {
        return publishObservers.Subscribe(observer);
    }

    protected override void OnPublish(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!PublishPacket.TryReadPayload(in reminder, header, (int)reminder.Length, out var id, out var topic, out var payload))
        {
            throw new InvalidDataException(string.Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
        }

        var qosLevel = (byte)((header >> 1) & QoSMask);
        var retain = (header & Retain) == Retain;

        switch(qosLevel)
        {
            case 0:
                DispatchMessage(topic, payload, retain);
                break;

            case 1:
                DispatchMessage(topic, payload, retain);
                PostRaw(PubAckPacketMask | id);
                break;

            case 2:
                if(sessionState.TryAddQoS2(id))
                {
                    DispatchMessage(topic, payload, retain);
                }

                PostRaw(PubRecPacketMask | id);
                break;

            default:
                throw new InvalidDataException(string.Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
        }
    }

    protected override void OnPubRel(byte header, ReadOnlySequence<byte> reminder)
    {
        if(!TryReadUInt16(in reminder, out var id))
        {
            throw new InvalidDataException(string.Format(InvariantCulture, InvalidPacketFormat, "PUBREL"));
        }

        sessionState.RemoveQoS2(id);

        PostRaw(PubCompPacketMask | id);
    }

    private void DispatchMessage(string topic, ReadOnlyMemory<byte> payload, bool retained)
    {
        incomingQueueWriter.TryWrite(new MqttMessage(topic, payload, retained));
    }

#pragma warning disable CA1031 // Do not catch general exception types - method should not throw by design
    private async Task DispatchMessageAsync(CancellationToken cancellationToken)
    {
        var message = await incomingQueueReader.ReadAsync(cancellationToken).ConfigureAwait(false);

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
#pragma warning restore

#pragma warning disable CA1003 // Use generic event handler instances
    public event MessageReceivedHandler MessageReceived;
#pragma warning restore CA1003
}