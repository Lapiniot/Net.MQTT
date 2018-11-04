using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        protected override bool OnPublish(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PublishPacket.TryParse(buffer, out var packet, out consumed))
            {
                var message = new Message(packet.Topic, packet.Payload, packet.QoSLevel, packet.Retain);

                switch(packet.QoSLevel)
                {
                    case AtMostOnce:
                    {
                        OnMessageReceived(message);
                        break;
                    }
                    case AtLeastOnce:
                    {
                        OnMessageReceived(message);
                        SendPubAckAsync(packet.Id);
                        break;
                    }
                    case ExactlyOnce:
                    {
                        // This is to avoid message duplicating for QoS 2 packets
                        if(state.TryAddQoS2(packet.Id))
                        {
                            OnMessageReceived(message);
                        }

                        SendPubRecAsync(packet.Id);
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        protected override bool OnPubAck(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
            //if (resendQueue.TryRemove(packetId, out _))
            //{
            //    idPool.Return(packetId);
            //}
        }

        protected override bool OnPubRec(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PubRecPacket.TryParse(buffer,out var id))
            {
                var pubRelPacket = new PubRelPacket(id);
                state.AddToResendQueue(id, pubRelPacket);
                SendPubRelAsync(id);
                consumed = 4;
                return true;
            }

            consumed = 0;
            return false;
        }

        protected override bool OnPubRel(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PubRelPacket.TryParse(buffer, out var id))
            {
                state.RemoveQoS2(id);
                consumed = 4;
                SendPubCompAsync(id);
                return true;
            }

            consumed = 0;
            return false;
        }

        protected override bool OnPubComp(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PubCompPacket.TryParse(buffer, out var id))
            {
                state.RemoveFromResendQueue(id);
                consumed = 4;
                return true;
            }

            consumed = 0;
            return false;
        }

        public ValueTask<int> SendPublishAsync(PublishPacket packet, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(packet, cancellationToken);
        }

        public ValueTask<int> SendPublishAsync(string topic, in Memory<byte> payload, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new PublishPacket(0, default, topic, payload), cancellationToken);
        }

        public ValueTask<int> SendPublishAsync(ushort id, QoSLevel qosLevel, string topic,
            in Memory<byte> payload, in CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new PublishPacket(id, qosLevel, topic, payload), cancellationToken);
        }

        public ValueTask<int> SendPubAckAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PacketType.PubAck, id, cancellationToken);
        }

        public ValueTask<int> SendPubRecAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PacketType.PubRec, id, cancellationToken);
        }

        public ValueTask<int> SendPubRelAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PacketType.PubRel, id, cancellationToken);
        }

        public ValueTask<int> SendPubCompAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PacketType.PubComp, id, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<int> SendPublishResponseAsync(PacketType type, ushort id, CancellationToken cancellationToken)
        {
            return SendPacketAsync(new byte[] {(byte)type, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
        }

        //internal void Dispatch(string topic, Memory<byte> payload, QoSLevel qosLevel)
        //{
        //    switch (qosLevel)
        //    {
        //        case AtMostOnce:
        //            handler.PublishAsync(topic, payload);
        //            break;
        //        case AtLeastOnce:
        //        case ExactlyOnce:
        //        {
        //            var id = idPool.Rent();
        //            var packet = new PublishPacket(id, qosLevel, topic, payload);
        //            if (resendQueue.TryAdd(id, packet))
        //            {
        //                handler.PublishAsync(packet);
        //            }

        //            break;
        //        }
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(qosLevel), qosLevel, null);
        //    }
        //}
    }
}