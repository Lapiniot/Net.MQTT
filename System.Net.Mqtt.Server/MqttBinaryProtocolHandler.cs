using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server
{
    public class MqttBinaryProtocolHandler : NetworkStreamParser
    {
        private static readonly byte[] PingRespPacket = {(byte)PingResp, 0};
        private readonly IMqttPacketServerHandler packetHandler;

        public MqttBinaryProtocolHandler(INetworkTransport transport, IMqttPacketServerHandler packetHandler) :
            base(transport)
        {
            this.packetHandler = packetHandler ?? throw new ArgumentNullException(nameof(packetHandler));
        }

        protected override void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;

            //TODO: optimization: analyze only header byte and let TryParse implementations do validation
            if(TryParseHeader(buffer, out var header, out var length, out _))
            {
                var total = GetLengthByteCount(length) + 1 + length;

                if(total <= buffer.Length)
                {
                    var packetType = (PacketType)(header & PacketFlags.TypeMask);
                    switch(packetType)
                    {
                        case Connect:
                        {
                            if(ConnectPacketV4.TryParse(buffer, out var packet)) packetHandler.OnConnect(packet);

                            break;
                        }
                        case Publish:
                        {
                            if(PublishPacket.TryParse(buffer, out var packet)) packetHandler.OnPublish(packet);

                            break;
                        }
                        case PubAck:
                        {
                            if(PubAckPacket.TryParse(buffer, out var id)) packetHandler.OnPubAck(id);

                            break;
                        }
                        case PubRec:
                        {
                            if(PubRecPacket.TryParse(buffer, out var id)) packetHandler.OnPubRec(id);

                            break;
                        }
                        case PubRel:
                        {
                            if(PubRelPacket.TryParse(buffer, out var id)) packetHandler.OnPubRel(id);

                            break;
                        }
                        case PubComp:
                        {
                            if(PubCompPacket.TryParse(buffer, out var id)) packetHandler.OnPubComp(id);

                            break;
                        }
                        case Subscribe:
                        {
                            if(SubscribePacket.TryParse(buffer, out var packet)) packetHandler.OnSubscribe(packet);

                            break;
                        }
                        case Unsubscribe:
                        {
                            if(UnsubscribePacket.TryParse(buffer, out var packet)) packetHandler.OnUnsubscribe(packet);

                            break;
                        }
                        case PingReq:
                        {
                            packetHandler.OnPingReq();

                            break;
                        }
                        case Disconnect:
                        {
                            packetHandler.OnDisconnect();
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    consumed = total;
                }
            }
        }

        protected override void OnEndOfStream()
        {
        }

        protected override void OnConnectionAborted()
        {
        }

        public Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            return SendAsync(packet.GetBytes(), cancellationToken);
        }

        public Task SendPacketAsync(byte[] packet, CancellationToken cancellationToken)
        {
            return SendAsync(packet, cancellationToken);
        }

        public Task SendConnAckAsync(byte statusCode, bool sessionPresent, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new ConnAckPacket(statusCode, sessionPresent), cancellationToken);
        }

        public Task SendPingRespAsync(CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(PingRespPacket, cancellationToken);
        }

        public Task SendSubAckAsync(ushort id, byte[] result, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new SubAckPacket(id, result), cancellationToken);
        }

        public Task SendUnsubAckAsync(ushort id, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new byte[] {(byte)UnsubAck, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
        }

        public Task PublishAsync(PublishPacket packet, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(packet, cancellationToken);
        }

        public Task PublishAsync(string topic, in Memory<byte> payload, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new PublishPacket(0, default, topic, payload), cancellationToken);
        }

        public Task PublishAsync(ushort id, QoSLevel qosLevel, string topic,
            in Memory<byte> payload, in CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new PublishPacket(id, qosLevel, topic, payload), cancellationToken);
        }

        public Task SendPubAckAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubAck, id, cancellationToken);
        }

        public Task SendPubRecAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubRec, id, cancellationToken);
        }

        public Task SendPubRelAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubRel, id, cancellationToken);
        }

        public Task SendPubCompAsync(ushort id, in CancellationToken cancellationToken = default)
        {
            return SendPublishResponseAsync(PubComp, id, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task SendPublishResponseAsync(PacketType type, ushort id, CancellationToken cancellationToken)
        {
            return SendPacketAsync(new byte[] {(byte)type, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
        }
    }
}