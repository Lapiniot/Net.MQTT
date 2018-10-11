using System.Buffers;
using System.Net.Mqtt.Packets;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Broker
{
    public class MqttBinaryProtocolHandler : NetworkStreamParser
    {
        private readonly IMqttPacketServerHandler packetHandler;

        public MqttBinaryProtocolHandler(INetworkTransport transport, IMqttPacketServerHandler packetHandler) :
            base(transport)
        {
            this.packetHandler = packetHandler ?? throw new ArgumentNullException(nameof(packetHandler));
        }

        protected override void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;

            if(TryParseHeader(buffer, out var header, out var length, out _))
            {
                var total = GetLengthByteCount(length) + 1 + length;

                if(total <= buffer.Length)
                {
                    var packetType = (PacketType)(header & PacketFlags.TypeMask);
                    switch(packetType)
                    {
                        case PacketType.Connect:
                        {
                            if(ConnectPacket.TryParse(buffer, out var packet)) packetHandler.OnConnect(this, packet);

                            break;
                        }
                        case PacketType.Publish:
                        {
                            if(PublishPacket.TryParse(buffer, out var packet)) packetHandler.OnPublish(this, packet);

                            break;
                        }
                        case PacketType.PubAck:
                        {
                            if(PubAckPacket.TryParse(buffer, out var packet)) packetHandler.OnPubAck(this, packet);

                            break;
                        }
                        case PacketType.PubRec:
                        {
                            if(PubRecPacket.TryParse(buffer, out var packet)) packetHandler.OnPubRec(this, packet);

                            break;
                        }
                        case PacketType.PubRel:
                        {
                            if(PubRelPacket.TryParse(buffer, out var packet)) packetHandler.OnPubRel(this, packet);

                            break;
                        }
                        case PacketType.PubComp:
                        {
                            if(PubCompPacket.TryParse(buffer, out var packet)) packetHandler.OnPubComp(this, packet);

                            break;
                        }
                        case PacketType.Subscribe:
                        {
                            if(SubscribePacket.TryParse(buffer, out var packet)) packetHandler.OnSubscribe(this, packet);

                            break;
                        }
                        case PacketType.Unsubscribe:
                        {
                            if(UnsubscribePacket.TryParse(buffer, out var packet)) packetHandler.OnUnsubscribe(this, packet);

                            break;
                        }
                        case PacketType.PingReq:
                        {
                            packetHandler.OnPingReq(this);

                            break;
                        }
                        case PacketType.Disconnect:
                        {
                            packetHandler.OnDisconnect(this);
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
    }
}