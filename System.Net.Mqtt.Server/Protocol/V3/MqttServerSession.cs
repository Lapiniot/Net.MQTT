using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Packets.ConnAckPacket;
using static System.Net.Mqtt.Properties.Strings;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class MqttServerSession : Server.MqttServerSession
    {
        private static readonly PingRespPacket PingRespPacket = new PingRespPacket();
        private readonly WorkerLoop messageWorker;
        private readonly ISessionStateRepository<MqttServerSessionState> repository;
        private readonly IMqttServer server;
        private DelayWorkerLoop pingWatch;
        private MqttServerSessionState state;
        private Message willMessage;

        public MqttServerSession(IMqttServer server, INetworkConnection connection, PipeReader reader,
            ISessionStateRepository<MqttServerSessionState> stateRepository, ILogger logger) :
            base(server, connection, reader, logger)
        {
            this.server = server;
            repository = stateRepository;
            messageWorker = new WorkerLoop(ProcessMessageAsync);
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
                    SetWillMessage(new Message(packet.WillTopic, packet.WillMessage, packet.WillQoS, packet.WillRetain));
                }
            }
            else
            {
                throw new InvalidDataException(ConnectPacketExpected);
            }
        }

        protected override void OnPacketSent() {}

        protected override async Task StartingAsync(CancellationToken cancellationToken)
        {
            if(!ConnectionAccepted) throw new InvalidOperationException(CannotConnectBeforeAccept);

            state = repository.GetOrCreate(ClientId, CleanSession, out var existing);

            await AcknowledgeConnection(existing, cancellationToken).ConfigureAwait(false);

            foreach(var packet in state.GetResendPackets()) Post(packet);

            await base.StartingAsync(cancellationToken).ConfigureAwait(false);

            state.IsActive = true;

            state.WillMessage = willMessage;

            if(KeepAlive > 0)
            {
                pingWatch = new DelayWorkerLoop(NoPingDisconnectAsync, TimeSpan.FromSeconds(KeepAlive * 1.5), 1);

                pingWatch.Start();
            }

            messageWorker.Start();
        }

        protected virtual ValueTask<int> AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(new byte[] {0b0010_0000, 2, 0, Accepted}, cancellationToken);
        }

        protected override async Task StoppingAsync()
        {
            try
            {
                if(state.WillMessage != null)
                {
                    OnMessageReceived(state.WillMessage);
                    state.WillMessage = null;
                }

                if(pingWatch != null)
                {
                    await pingWatch.StopAsync().ConfigureAwait(false);
                }

                await messageWorker.StopAsync().ConfigureAwait(false);

                await base.StoppingAsync().ConfigureAwait(false);
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

        protected override void OnCompleted(Exception exception = null) {}

        protected override void OnConnect(byte header, ReadOnlySequence<byte> buffer)
        {
            throw new NotSupportedException();
        }

        protected override void OnPingReq(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b1100_0000) throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PINGREQ"));

            Post(PingRespPacket);
        }

        protected override void OnDisconnect(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b1110_0000) throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "DISCONNECT"));

            // Graceful disconnection: no need to dispatch last will message
            state.WillMessage = null;

            var _ = StopAsync();
        }

        private Task NoPingDisconnectAsync(CancellationToken cancellationToken)
        {
            var _ = StopAsync();
            return Task.CompletedTask;
        }

        protected override void OnPacketReceived()
        {
            pingWatch?.ResetDelay();
        }

        public override async ValueTask DisposeAsync()
        {
            await using(messageWorker.ConfigureAwait(false))
            await using(pingWatch.ConfigureAwait(false))
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        protected void SetWillMessage(Message message)
        {
            willMessage = message;
        }
    }
}