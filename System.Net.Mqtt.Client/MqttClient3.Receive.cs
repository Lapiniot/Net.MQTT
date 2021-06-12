using System.Buffers;
using System.IO;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient3
    {
        private readonly ChannelReader<MqttMessage> incomingQueueReader;
        private readonly ChannelWriter<MqttMessage> incomingQueueWriter;
#pragma warning disable CA2213 // Disposable fields should be disposed: Warning is wrongly emitted due to some issues with analyzer itself
        private readonly WorkerLoop dispatcher;
#pragma warning disable CA2213
        private readonly ObserversContainer<MqttMessage> publishObservers;

        public IDisposable Subscribe(IObserver<MqttMessage> observer)
        {
            return publishObservers.Subscribe(observer);
        }

        protected override void OnPublish(byte header, ReadOnlySequence<byte> reminder)
        {
            if(!PublishPacket.TryReadPayload(header, (int)reminder.Length, reminder, out var packet))
            {
                throw new InvalidDataException(string.Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
            }

            switch(packet.QoSLevel)
            {
                case 0:
                    DispatchMessage(packet.Topic, packet.Payload, packet.Retain);
                    break;

                case 1:
                    DispatchMessage(packet.Topic, packet.Payload, packet.Retain);
                    Post(new PubAckPacket(packet.Id));
                    break;

                case 2:
                    if(sessionState.TryAddQoS2(packet.Id))
                    {
                        DispatchMessage(packet.Topic, packet.Payload, packet.Retain);
                    }

                    Post(new PubRecPacket(packet.Id));
                    break;

                default:
                    throw new InvalidDataException(string.Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
            }
        }

        protected override void OnPubRel(byte header, ReadOnlySequence<byte> reminder)
        {
            if(!reminder.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(string.Format(InvariantCulture, InvalidPacketFormat, "PUBREL"));
            }

            sessionState.RemoveQoS2(id);

            Post(new PubCompPacket(id));
        }

        private void DispatchMessage(string topic, Memory<byte> payload, bool retained)
        {
            incomingQueueWriter.TryWrite(new MqttMessage(topic, payload, retained));
        }

        private async Task DispatchMessageAsync(CancellationToken cancellationToken)
        {
            var message = await incomingQueueReader.ReadAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message.Topic, message.Payload, message.Retained));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                //ignore
            }

            publishObservers.Notify(message);
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
    }
}