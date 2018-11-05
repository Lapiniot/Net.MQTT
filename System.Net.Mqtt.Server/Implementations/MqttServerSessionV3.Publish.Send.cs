using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        protected override bool OnPubAck(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PubAckPacket.TryParse(buffer, out var id))
            {
                consumed = 4;
                state.RemoveResendPacket(id);
            }

            consumed = 0;
            return false;
        }

        protected override bool OnPubRec(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PubRecPacket.TryParse(buffer, out var id))
            {
                var pubRelPacket = new PubRelPacket(id);
                state.UpdateResendPacket(id, pubRelPacket);
                SendPubRelAsync(id);
                consumed = 4;
                return true;
            }

            consumed = 0;
            return false;
        }

        protected override bool OnPubComp(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(PubCompPacket.TryParse(buffer, out var id))
            {
                state.RemoveResendPacket(id);
                consumed = 4;
                return true;
            }

            consumed = 0;
            return false;
        }

        public ValueTask<int> SendPubRelAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubRel, id, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<int> SendPublishResponseAsync(PacketType type, ushort id, CancellationToken cancellationToken)
        {
            return SendPacketAsync(new byte[] {(byte)type, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
        }

        internal void Dispatch(string topic, Memory<byte> payload, QoSLevel qosLevel)
        {
            switch(qosLevel)
            {
                case AtMostOnce:
                    SendPacketAsync(new PublishPacket(0, default, topic, payload), default);
                    break;
                case AtLeastOnce:
                case ExactlyOnce:
                {
                    var packet = state.AddResendPacket(id => new PublishPacket(id, qosLevel, topic, payload));
                    SendPacketAsync(packet, default);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(qosLevel), qosLevel, null);
            }
        }
    }
}