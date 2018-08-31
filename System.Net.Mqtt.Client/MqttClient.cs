using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Net.Mqtt.Messages;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketFlags;

namespace System.Net.Mqtt.Client
{
    public class MqttClient : AsyncConnectedObject<MqttConnectionOptions>
    {
        private Pipe pipe;
        private Task processor;
        private CancellationTokenSource processorCts;
        private Socket socket;
        private volatile int currentId = 0;

        ConcurrentDictionary<ushort, MqttMessage> map = new ConcurrentDictionary<ushort, MqttMessage>();

        public MqttClient(IPEndPoint endpoint, string clientId)
        {
            Endpoint = endpoint;
            ClientId = clientId;
        }

        public MqttClient(IPEndPoint endpoint) :
            this(endpoint, Path.GetRandomFileName())
        {
        }

        public string ClientId { get; }

        public IPEndPoint Endpoint { get; }

        protected override async Task OnConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            socket = new Socket(InterNetwork, SocketType.Stream, Tcp);
            await socket.ConnectAsync(Endpoint);

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

            await socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);

            var buffer = new byte[4];
            var received = await socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
            new ConnAckMessage(buffer.AsSpan(0, received)).EnsureSuccessStatusCode();

            pipe = new Pipe(new PipeOptions(minimumSegmentSize: 512));
            processorCts = new CancellationTokenSource();
            var token = processorCts.Token;
            processor = Task.WhenAll(
                Task.Run(() => StartNetworkReader(pipe.Writer, token), token),
                Task.Run(() => StartParser(pipe.Reader, token), token));
        }

        private async Task StartNetworkReader(PipeWriter writer, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var received = await socket.ReceiveAsync(buffer, None, token).ConfigureAwait(false);

                    if(received == 0) break;

                    writer.Advance(received);

                    var result = await writer.FlushAsync(token).ConfigureAwait(false);

                    if(result.IsCompleted) break;
                }
            }
            catch(OperationCanceledException)
            {
            }
            finally
            {
                writer.Complete();
            }
        }

        private async Task StartParser(PipeReader reader, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var result = await reader.ReadAsync(token).ConfigureAwait(false);

                    var buffer = result.Buffer;

                    if(buffer.IsEmpty) continue;

                    ParseBuffer(buffer, out var consumed);

                    reader.AdvanceTo(buffer.GetPosition(consumed));

                    if(result.IsCompleted) break;
                }
            }
            catch(OperationCanceledException)
            {
            }
            finally
            {
                reader.Complete();
            }
        }

        private void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;

            if(MqttHelpers.TryParseHeader(buffer, out var header, out var length))
            {
                var total = MqttHelpers.GetLengthByteCount(length) + 1 + length;

                if(total <= buffer.Length)
                {
                    var packetType = (PacketType)(header & TypeMask);

                    switch(packetType)
                    {
                        case PacketType.Publish:
                            break;
                        case PacketType.PubAck:
                            break;
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

        protected override async Task OnCloseAsync()
        {
            processorCts.Cancel();
            await processor.ConfigureAwait(false);

            await socket.SendAsync(new byte[] { (byte)PacketType.Disconnect, 0 }, None, default);

            socket.Disconnect(false);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public async Task PublishAsync(string topic, Memory<byte> payload,
            QoSLevel qosLevel = AtMostOnce, bool retain = false, CancellationToken token = default)
        {
            CheckConnected();

            var message = new PublishMessage(topic, payload) { QoSLevel = qosLevel, Retain = retain };
            if(qosLevel != QoSLevel.AtMostOnce) message.PacketId = GenerateId();

            await socket.SendAsync(message.GetBytes(), None, token).ConfigureAwait(false);
            
            if(qosLevel == QoSLevel.AtLeastOnce)
            {
                map.AddOrUpdate(message.PacketId, message, (Id, m1) => m1);
            }
        }

        private ushort GenerateId()
        {
            return (ushort)Interlocked.Increment(ref currentId);
        }
    }
}