using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Mqtt.Messages;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Sockets.SocketFlags;

namespace System.Net.Mqtt.Client
{
    public class MqttClient : NetworkStreamParser<MqttConnectionOptions>
    {
        private readonly UInt16IdentityPool idPool = new UInt16IdentityPool(1);
        private readonly ConcurrentDictionary<ushort, MqttMessage> map = new ConcurrentDictionary<ushort, MqttMessage>();

        public MqttClient(IPEndPoint endpoint, string clientId) : base(endpoint)
        {
            ClientId = clientId;
        }

        public MqttClient(IPEndPoint endpoint) :
            this(endpoint, Path.GetRandomFileName())
        {
        }

        public string ClientId { get; }

        public async Task PublishAsync(string topic, Memory<byte> payload,
            QoSLevel qosLevel = AtMostOnce, bool retain = false, CancellationToken token = default)
        {
            CheckConnected();

            var message = new PublishMessage(topic, payload) {QoSLevel = qosLevel, Retain = retain};

            if(qosLevel != AtMostOnce) message.PacketId = idPool.Rent();

            await Socket.SendAsync(message.GetBytes(), None, token).ConfigureAwait(false);

            if(qosLevel == AtLeastOnce)
            {
                map.TryAdd(message.PacketId, message);
            }
        }

        private async Task MqttConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            var message = new ConnectMessage(ClientId)
            {
                KeepAlive = options.KeepAlive,
                CleanSession = options.CleanSession,
                UserName = options.UserName,
                Password = options.Password,
                WillTopic = options.LastWillTopic,
                WillMessage = options.LastWillMessage,
                WillQoS = options.LastWillQoS,
                WillRetain = options.LastWillRetain
            };

            await Socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);

            var buffer = new byte[4];
            var received = await Socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
            new ConnAckMessage(buffer.AsSpan(0, received)).EnsureSuccessStatusCode();
        }

        private async Task MqttDisconnectAsync()
        {
            await Socket.SendAsync(new byte[] {(byte)PacketType.Disconnect, 0}, None, default).ConfigureAwait(false);
        }

        #region Overrides of NetworkStreamParser<MqttConnectionOptions>

        protected override void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;

            if(TryParseHeader(buffer, out var header, out var length))
            {
                var total = GetLengthByteCount(length) + 1 + length;

                if(total <= buffer.Length)
                {
                    var packetType = (PacketType)(header & TypeMask);

                    switch(packetType)
                    {
                        case PacketType.Publish:
                            break;
                        case PacketType.PubAck:
                        {
                            if(TryReadUInt16(buffer.Slice(2), out var packetId))
                            {
                                map.TryRemove(packetId, out _);
                                idPool.Return(packetId);
                            }

                            break;
                        }
                        case PacketType.PubRec:
                            break;
                        case PacketType.PubRel:
                            break;
                        case PacketType.PubComp:
                            break;
                        case PacketType.SubAck:
                            break;
                        case PacketType.UnsubAck:
                            break;
                        case PacketType.PingResp:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    consumed = total;
                }
            }
        }

        protected override async Task OnConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(options, cancellationToken).ConfigureAwait(false);
            await MqttConnectAsync(options, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnCloseAsync()
        {
            try
            {
                await MqttDisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            await base.OnCloseAsync().ConfigureAwait(false);
        }
    }

    #endregion
}