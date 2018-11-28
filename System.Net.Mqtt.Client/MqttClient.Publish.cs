using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.QoSLevel;
using static System.String;

namespace System.Net.Mqtt.Client
{
    public delegate void MessageReceivedHandler(MqttClient sender, MqttMessage message);

    public partial class MqttClient : IObservable<MqttMessage>
    {
        private readonly WorkerLoop<object> messageDispatcher;
        private readonly ChannelReader<MqttMessage> messageQueueReader;
        private readonly ChannelWriter<MqttMessage> messageQueueWriter;
        private readonly HashQueue<ushort, MqttPacket> publishFlowPackets;
        private readonly ObserversContainer<MqttMessage> publishObservers;
        private readonly Dictionary<ushort, MqttPacket> receiveFlowPackets;

        IDisposable IObservable<MqttMessage>.Subscribe(IObserver<MqttMessage> observer)
        {
            return publishObservers.Subscribe(observer);
        }

        private async Task DispatchMessageAsync(object state, CancellationToken cancellationToken)
        {
            var message = await messageQueueReader.ReadAsync(cancellationToken).ConfigureAwait(false);

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

        private void DispatchMessage(string topic, Memory<byte> payload)
        {
            messageQueueWriter.TryWrite(new MqttMessage(topic, payload));
        }

        public async Task PublishAsync(string topic, Memory<byte> payload,
            QoSLevel qosLevel = AtMostOnce, bool retain = false,
            CancellationToken token = default)
        {
            CheckConnected();

            PublishPacket packet;

            if(qosLevel == AtLeastOnce || qosLevel == ExactlyOnce)
            {
                var id = idPool.Rent();

                packet = new PublishPacket(id, (byte)qosLevel, topic, payload, retain);

                var registered = publishFlowPackets.TryAdd(id, packet);

                Debug.Assert(registered, "Cannot register publish packet for QoS (L1,L2).");
            }
            else
            {
                packet = new PublishPacket(0, 0, topic, payload, retain);
            }

            await MqttSendPacketAsync(packet, token).ConfigureAwait(false);
        }

        protected override void OnPublish(byte header, ReadOnlySequence<byte> remainder)
        {
            if((header & 0b11_0000) != 0b11_0000 || !PublishPacket.TryParsePayload(header, remainder, out var packet))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBLISH"));
            }

            switch(packet.QoSLevel)
            {
                case 0:
                {
                    DispatchMessage(packet.Topic, packet.Payload);
                    break;
                }
                case 1:
                {
                    DispatchMessage(packet.Topic, packet.Payload);
                    var pubAckPacket = new PubAckPacket(packet.Id);
                    MqttSendPacketAsync(pubAckPacket);
                    break;
                }
                case 2:
                {
                    if(receiveFlowPackets.TryAdd(packet.Id, null))
                    {
                        DispatchMessage(packet.Topic, packet.Payload);
                    }

                    var pubRecPacket = new PubRecPacket(packet.Id);
                    MqttSendPacketAsync(pubRecPacket);
                    break;
                }
            }
        }

        protected override void OnPubAck(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0100_0000 || !TryReadUInt16(remainder, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBACK"));
            }

            if(publishFlowPackets.TryRemove(id, out _))
            {
                idPool.Return(id);
            }
        }

        protected override void OnPubRec(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0101_0000 || !TryReadUInt16(remainder, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBREC"));
            }

            var pubRelPacket = new PubRelPacket(id);

            publishFlowPackets.AddOrUpdate(id, pubRelPacket, (id1, _) => pubRelPacket);

            MqttSendPacketAsync(pubRelPacket);
        }

        protected override void OnPubRel(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0110_0000 || !TryReadUInt16(remainder, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBREL"));
            }

            receiveFlowPackets.Remove(id);

            MqttSendPacketAsync(new PubCompPacket(id));
        }

        protected override void OnPubComp(byte header, ReadOnlySequence<byte> remainder)
        {
            if(header != 0b0111_0000 || !TryReadUInt16(remainder, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBCOMP"));
            }

            if(publishFlowPackets.TryRemove(id, out _))
            {
                idPool.Return(id);
            }
        }
    }
}