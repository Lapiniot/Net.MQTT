using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket.StatusCodes;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession : MqttServerSession
    {
        private static readonly PingRespPacket PingRespPacket = new PingRespPacket();
        private readonly WorkerLoop<object> messageWorker;
        private readonly ISessionStateRepository<SessionState> repository;
        private DelayWorkerLoop<object> pingWatch;
        private SessionState state;
        protected Message WillMessage;

        public ServerSession(IMqttServer server, INetworkTransport transport, PipeReader reader,
            ISessionStateRepository<SessionState> stateRepository, ILogger logger) :
            base(server, transport, reader, logger)
        {
            repository = stateRepository;
            messageWorker = new WorkerLoop<object>(ProcessMessageAsync, null);
        }

        public bool CleanSession { get; protected set; }

        public ushort KeepAlive { get; protected set; }

        protected override async Task OnAcceptConnectionAsync(CancellationToken cancellationToken)
        {
            var rt = ReadPacketAsync(cancellationToken);
            var sequence = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

            if(ConnectPacket.TryRead(sequence, out var packet, out _))
            {
                if(packet.ProtocolLevel != 0x03)
                {
                    await Transport.SendAsync(new byte[] {0b0010_0000, 2, 0, ProtocolRejected}, cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(NotSupportedProtocol);
                }

                if(IsNullOrEmpty(packet.ClientId) || packet.ClientId.Length > 23)
                {
                    await Transport.SendAsync(new byte[] {0b0010_0000, 2, 0, IdentifierRejected}, cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(InvalidClientIdentifier);
                }

                CleanSession = packet.CleanSession;
                ClientId = packet.ClientId;
                KeepAlive = packet.KeepAlive;

                if(!IsNullOrEmpty(packet.WillTopic))
                {
                    WillMessage = new Message(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain);
                }
            }
            else
            {
                throw new InvalidDataException(ConnectPacketExpected);
            }
        }

        protected override void OnPacketSent() {}

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            if(!ConnectionAccepted) throw new InvalidOperationException(CannotConnectBeforeAccept);

            state = repository.GetOrCreate(ClientId, CleanSession);

            await Transport.SendAsync(new byte[] {0b0010_0000, 2, 0, Accepted}, cancellationToken).ConfigureAwait(false);

            foreach(var packet in state.GetResendPackets()) Post(packet);

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

            state.IsActive = true;

            state.WillMessage = WillMessage;

            if(KeepAlive > 0)
            {
                pingWatch = new DelayWorkerLoop<object>(NoPingDisconnectAsync, null, TimeSpan.FromSeconds(KeepAlive * 1.5), 1);

                pingWatch.Start();
            }

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

                await base.OnDisconnectAsync().ConfigureAwait(false);
            }
            finally
            {
                if(CleanSession)
                {
                    repository.Remove(ClientId);
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
            if(header != 0b1100_0000) throw new InvalidDataException(Format(InvalidPacketFormat, "PINGREQ"));

            Post(PingRespPacket);
        }

        protected override void OnDisconnect(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b1110_0000) throw new InvalidDataException(Format(InvalidPacketFormat, "DISCONNECT"));

            // Graceful disconnection: no need to dispatch last will message
            state.WillMessage = null;

            var _ = DisconnectAsync();
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
                pingWatch?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}