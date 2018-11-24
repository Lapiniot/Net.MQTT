using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Properties;
using System.Net.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession : MqttServerSession<SessionState>
    {
        private static readonly byte[] PingRespPacket = {0xD0, 0x00};
        private readonly WorkerLoop<object> dispatcher;
        private DelayWorkerLoop<object> pingWatch;
        private SessionState state;

        public ServerSession(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionState> stateProvider, IObserver<Message> observer) :
            base(transport, reader, stateProvider, observer)
        {
            dispatcher = new WorkerLoop<object>(DispatchMessageAsync, null);
        }

        public bool CleanSession { get; set; }

        public ushort KeepAlive { get; private set; }

        protected override async Task OnAcceptConnectionAsync(CancellationToken cancellationToken)
        {
            var vt = MqttPacketHelpers.ReadPacketAsync(Reader, cancellationToken);

            var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);

            if(ConnectPacketV3.TryParse(result.Buffer, out var packet, out var consumed))
            {
                if(packet.ProtocolLevel != ConnectPacketV3.Level)
                {
                    await SendPacketAsync(new ConnAckPacket(ConnAckPacket.StatusCodes.ProtocolRejected), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(Strings.NotSupportedProtocol);
                }

                if(string.IsNullOrEmpty(packet.ClientId) || packet.ClientId.Length > 23)
                {
                    await SendPacketAsync(new ConnAckPacket(ConnAckPacket.StatusCodes.IdentifierRejected), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(Strings.InvalidClientIdentifier);
                }

                Reader.AdvanceTo(result.Buffer.GetPosition(consumed));

                CleanSession = packet.CleanSession;
                ClientId = packet.ClientId;
                KeepAlive = packet.KeepAlive;
            }
            else
            {
                throw new InvalidDataException(Strings.ConnectPacketExpected);
            }
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            if(!ConnectionAccepted) throw new InvalidOperationException(Strings.CannotConnectBeforeAccept);

            state = StateProvider.GetOrCreate(ClientId, CleanSession);

            await SendPacketAsync(new ConnAckPacket(ConnAckPacket.StatusCodes.Accepted), cancellationToken).ConfigureAwait(false);

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

            state.IsActive = true;

            if(KeepAlive > 0)
            {
                pingWatch = new DelayWorkerLoop<object>(NoPingDisconnectAsync, null, TimeSpan.FromSeconds(KeepAlive * 1.5), 1);

                pingWatch.Start();
            }

            foreach(var p in state.GetResendPackets())
            {
                await SendPacketAsync(p, cancellationToken).ConfigureAwait(true);
            }

            dispatcher.Start();
        }

        protected override async Task OnDisconnectAsync()
        {
            try
            {
                pingWatch?.Stop();
                dispatcher.Stop();
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

        protected override Task OnConnectAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected override Task OnPingReqAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b1100_0000) throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "PINGREQ"));

            return SendPacketAsync(PingRespPacket, cancellationToken);
        }

        protected override Task OnDisconnectAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b1110_0000) throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "DISCONNECT"));

            var _ = DisconnectAsync();

            return Transport.DisconnectAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task SendPublishResponseAsync(PacketType type, ushort id, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new byte[] {(byte)type, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
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
                dispatcher.Dispose();
                pingWatch?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}