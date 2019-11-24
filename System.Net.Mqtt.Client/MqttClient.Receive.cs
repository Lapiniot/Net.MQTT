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
    public partial class MqttClient
    {
        private readonly ChannelReader<MqttMessage> incomingQueueReader;
        private readonly ChannelWriter<MqttMessage> incomingQueueWriter;
        private readonly WorkerLoop messageDispatcher;
        private readonly ObserversContainer<MqttMessage> publishObservers;

        public IDisposable Subscribe(IObserver<MqttMessage> observer)
        {
            return publishObservers.Subscribe(observer);
        }

        protected override void OnPublish(byte header, ReadOnlySequence<byte> remainder)
        {
            if((header & 0b11_0000) != 0b11_0000 || !PublishPacket.TryReadPayload(header, (int)remainder.Length, remainder, out var packet))
            {
                throw new InvalidDataException(string.Format(InvariantCulture, InvalidPacketFormat, "PUBLISH"));
            }

            switch(packet.QoSLevel)
            {
                case 0:
                {
                    DispatchMessage(packet.Topic, packet.Payload, packet.Retain);
                    break;
                }

                case 1:
                {
                    DispatchMessage(packet.Topic, packet.Payload, packet.Retain);

                    Post(new PubAckPacket(packet.Id));

                    break;
                }

                case 2:
                {
                    if(sessionState.TryAddQoS2(packet.Id))
                    {
                        DispatchMessage(packet.Topic, packet.Payload, packet.Retain);
                    }

                    Post(new PubRecPacket(packet.Id));

                    break;
                }
            }
        }

        protected override void OnPubRel(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0110_0000 || !remainder.TryReadUInt16(out var id))
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
                MessageReceived?.Invoke(this, message);
            }
            catch
            {
                //ignore
            }

            publishObservers.Notify(message);
        }

        public event MessageReceivedHandler MessageReceived;
    }
}