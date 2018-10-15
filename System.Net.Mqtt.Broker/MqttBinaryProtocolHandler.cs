using System.Buffers;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Broker
{
    public class MqttBinaryProtocolHandler : NetworkStreamParser
    {
        private static readonly byte[] PingRespPacket = { (byte)PacketType.PingResp, 0 };
        private readonly IMqttPacketServerHandler packetHandler;

        public string ClientId { get; internal set; }

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
                                if(ConnectPacket.TryParse(buffer, out var packet)) packetHandler.OnConnect(packet);

                                break;
                            }
                        case PacketType.Publish:
                            {
                                if(PublishPacket.TryParse(buffer, out var packet)) packetHandler.OnPublish(packet);

                                break;
                            }
                        case PacketType.PubAck:
                            {
                                if(PubAckPacket.TryParse(buffer, out var packet)) packetHandler.OnPubAck(packet);

                                break;
                            }
                        case PacketType.PubRec:
                            {
                                if(PubRecPacket.TryParse(buffer, out var packet)) packetHandler.OnPubRec(packet);

                                break;
                            }
                        case PacketType.PubRel:
                            {
                                if(PubRelPacket.TryParse(buffer, out var packet)) packetHandler.OnPubRel(packet);

                                break;
                            }
                        case PacketType.PubComp:
                            {
                                if(PubCompPacket.TryParse(buffer, out var packet)) packetHandler.OnPubComp(packet);

                                break;
                            }
                        case PacketType.Subscribe:
                            {
                                if(SubscribePacket.TryParse(buffer, out var packet)) packetHandler.OnSubscribe(packet);

                                break;
                            }
                        case PacketType.Unsubscribe:
                            {
                                if(UnsubscribePacket.TryParse(buffer, out var packet)) packetHandler.OnUnsubscribe(packet);

                                break;
                            }
                        case PacketType.PingReq:
                            {
                                packetHandler.OnPingReq();

                                break;
                            }
                        case PacketType.Disconnect:
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

        public Task SendPacketAsync(MqttPacket packet, in CancellationToken cancellationToken)
        {
            return SendAsync(packet.GetBytes(), cancellationToken);
        }

        public Task SendPacketAsync(byte[] packet, in CancellationToken cancellationToken)
        {
            return SendAsync(packet, cancellationToken);
        }

        public Task SendConnAckAsync(byte statusCode, bool sessionPresent, in CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new ConnAckPacket(statusCode, sessionPresent), cancellationToken);
        }

        public Task SendPingRespAsync(in CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(PingRespPacket, cancellationToken);
        }

        protected override async Task OnDisconnectAsync()
        {
            try
            {
                await Transport.DisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            await base.OnDisconnectAsync().ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            Transport.Dispose();
            base.Dispose(disposing);
        }
    }
}