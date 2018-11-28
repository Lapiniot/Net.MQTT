using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.Packets.ConnAckPacket.StatusCodes;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession : MqttServerSession<SessionState>
    {
        private static readonly byte[] PingRespPacket = {0xD0, 0x00};
        private readonly WorkerLoop<object> messageWorker;
        private readonly ChannelReader<Memory<byte>> postQueueReader;
        private readonly ChannelWriter<Memory<byte>> postQueueWriter;
        private readonly WorkerLoop<object> postWorker;
        private DelayWorkerLoop<object> pingWatch;
        private SessionState state;
        private Message willMessage;

        public ServerSession(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionState> stateProvider, IMqttServer server) :
            base(transport, reader, stateProvider, server)
        {
            messageWorker = new WorkerLoop<object>(ProcessMessageAsync, null);
            postWorker = new WorkerLoop<object>(DispatchPacketAsync, null);

            var channel = Channel.CreateUnbounded<Memory<byte>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            postQueueReader = channel.Reader;
            postQueueWriter = channel.Writer;
        }

        public bool CleanSession { get; set; }

        public ushort KeepAlive { get; private set; }

        protected void Post(MqttPacket packet)
        {
            if(!postQueueWriter.TryWrite(packet.GetBytes())) throw new InvalidOperationException(CannotAddOutgoingPacket);
        }

        protected void Post(byte[] packet)
        {
            if(!postQueueWriter.TryWrite(packet)) throw new InvalidOperationException(CannotAddOutgoingPacket);
        }

        private async Task DispatchPacketAsync(object arg1, CancellationToken cancellationToken)
        {
            var rvt = postQueueReader.ReadAsync(cancellationToken);
            var buffer = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.AsTask().ConfigureAwait(false);

            var svt = SendAsync(buffer, cancellationToken);
            if(!svt.IsCompletedSuccessfully) await svt.ConfigureAwait(false);
        }

        protected override async Task OnAcceptConnectionAsync(CancellationToken cancellationToken)
        {
            var rt = ReadPacketAsync(cancellationToken);
            var sequence = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

            if(ConnectPacketV3.TryParse(sequence, out var packet, out _))
            {
                if(packet.ProtocolLevel != ConnectPacketV3.Level)
                {
                    await SendAsync(new ConnAckPacket(ProtocolRejected).GetBytes(), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(NotSupportedProtocol);
                }

                if(IsNullOrEmpty(packet.ClientId) || packet.ClientId.Length > 23)
                {
                    await SendAsync(new ConnAckPacket(IdentifierRejected).GetBytes(), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(InvalidClientIdentifier);
                }

                CleanSession = packet.CleanSession;
                ClientId = packet.ClientId;
                KeepAlive = packet.KeepAlive;

                if(!IsNullOrEmpty(packet.WillTopic))
                {
                    willMessage = new Message(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain);
                }
            }
            else
            {
                throw new InvalidDataException(ConnectPacketExpected);
            }
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            if(!ConnectionAccepted) throw new InvalidOperationException(CannotConnectBeforeAccept);

            state = StateProvider.GetOrCreate(ClientId, CleanSession);

            await SendAsync(new ConnAckPacket(Accepted), cancellationToken).ConfigureAwait(false);

            foreach(var packet in state.GetResendPackets())
            {
                Post(packet);
            }

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

            state.IsActive = true;

            state.WillMessage = willMessage;

            if(KeepAlive > 0)
            {
                pingWatch = new DelayWorkerLoop<object>(NoPingDisconnectAsync, null, TimeSpan.FromSeconds(KeepAlive * 1.5), 1);

                pingWatch.Start();
            }

            postWorker.Start();
            messageWorker.Start();
        }

        protected override async Task OnDisconnectAsync()
        {
            try
            {
                if(state.WillMessage != null)
                {
                    OnMessageReceived(state.WillMessage);
                    state.WillMessage = null;
                }

                pingWatch?.Stop();
                messageWorker.Stop();
                postWorker.Stop();
                await base.OnDisconnectAsync().ConfigureAwait(false);
            }
            finally
            {
                if(CleanSession)
                {
                    StateProvider.Remove(ClientId);
                }
                else
                {
                    state.IsActive = false;
                }
            }
        }

        protected override void OnConnect(byte header, ReadOnlySequence<byte> buffer)
        {
            throw new NotSupportedException();
        }

        protected override void OnPingReq(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b1100_0000) throw new InvalidDataException(Format(InvalidPacketTemplate, "PINGREQ"));

            Post(PingRespPacket);
        }

        protected override void OnDisconnect(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b1110_0000) throw new InvalidDataException(Format(InvalidPacketTemplate, "DISCONNECT"));

            // Graceful disconnection: no need to dispatch last will message
            state.WillMessage = null;

            var _ = DisconnectAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PostPublishResponse(PacketType type, ushort id)
        {
            Post(new byte[] {(byte)type, 2, (byte)(id >> 8), (byte)id});
        }

        private Task NoPingDisconnectAsync(object arg, CancellationToken cancellationToken)
        {
            var _ = DisconnectAsync();
            return Task.CompletedTask;
        }

        protected override void OnPacketReceived()
        {
            pingWatch?.ResetDelay();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                messageWorker.Dispose();
                postWorker.Dispose();
                pingWatch?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}