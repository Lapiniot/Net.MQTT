using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        protected override bool OnPublish(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubAck(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubRec(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubRel(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubComp(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
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
    }
}