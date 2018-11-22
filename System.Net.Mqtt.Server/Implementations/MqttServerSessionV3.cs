using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.Packets.ConnAckPacket.StatusCodes;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3 : MqttServerSession<SessionStateV3>
    {
        private static readonly byte[] PingRespPacket = {0xD0, 0x00};
        private readonly WorkerLoop<object> dispatcher;
        private DelayWorkerLoop<object> pingWatch;
        private SessionStateV3 state;

        public MqttServerSessionV3(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionStateV3> stateProvider, IObserver<Message> observer) :
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
                    await SendPacketAsync(new ConnAckPacket(ProtocolRejected), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(NotSupportedProtocol);
                }

                if(IsNullOrEmpty(packet.ClientId) || packet.ClientId.Length > 23)
                {
                    await SendPacketAsync(new ConnAckPacket(IdentifierRejected), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(InvalidClientIdentifier);
                }

                Reader.AdvanceTo(result.Buffer.GetPosition(consumed));

                CleanSession = packet.CleanSession;
                ClientId = packet.ClientId;
                KeepAlive = packet.KeepAlive;
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

            await SendPacketAsync(new ConnAckPacket(Accepted), cancellationToken).ConfigureAwait(false);

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

            state.IsActive = true;

            pingWatch = new DelayWorkerLoop<object>(NoPingDisconnectAsync, null, TimeSpan.FromSeconds(KeepAlive * 1.5), 1);

            pingWatch.Start();

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
                pingWatch.Stop();
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
            if(header != 0b1100_0000) throw new InvalidDataException(Format(InvalidPacketTemplate, "PINGREQ"));

            return SendPacketAsync(PingRespPacket, cancellationToken);
        }

        protected override Task OnDisconnectAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b1110_0000) throw new InvalidDataException(Format(InvalidPacketTemplate, "DISCONNECT"));

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
            pingWatch.ResetDelay();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                dispatcher.Dispose();
                pingWatch.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}