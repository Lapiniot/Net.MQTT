using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
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
        private readonly ISessionStateRepository<MqttServerSessionState> repository;
        private readonly IObserver<SubscriptionRequest> subscribeObserver;
#pragma warning disable CA2213 // Disposable fields should be disposed: Warning is wrongly emitted due to some issues with analyzer itself
        private readonly WorkerLoop messageWorker;
        private DelayWorkerLoop pingWatch;
#pragma warning restore CA2213
        private MqttServerSessionState state;
        private Message willMessage;

        public MqttServerSession(NetworkTransport transport, ISessionStateRepository<MqttServerSessionState> stateRepository,
            ILogger logger, IObserver<SubscriptionRequest> subscribeObserver, IObserver<MessageRequest> messageObserver) :
            base(transport, logger, messageObserver)
        {
            repository = stateRepository;
            this.subscribeObserver = subscribeObserver;
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
                    await Transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, ProtocolRejected }, cancellationToken).ConfigureAwait(false);
                    throw new UnsupportedProtocolVersionException(packet.ProtocolLevel);
                }

                if(IsNullOrEmpty(packet.ClientId) || packet.ClientId.Length > 23)
                {
                    await Transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, IdentifierRejected }, cancellationToken).ConfigureAwait(false);
                    throw new InvalidClientIdException();
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
                throw new MissingConnectPacketException();
            }
        }

        protected override void OnPacketSent() { }

        protected override async Task StartingAsync(CancellationToken cancellationToken)
        {
            if(!ConnectionAccepted) throw new InvalidOperationException(CannotConnectBeforeAccept);

            state = repository.GetOrCreate(ClientId, CleanSession, out var existing);

            await AcknowledgeConnection(existing, cancellationToken).ConfigureAwait(false);

            foreach(var packet in state.ResendPackets) Post(packet);

            await base.StartingAsync(cancellationToken).ConfigureAwait(false);

            state.IsActive = true;

            state.WillMessage = willMessage;

            if(KeepAlive > 0)
            {
                pingWatch = new DelayWorkerLoop(NoPingDisconnectAsync, TimeSpan.FromSeconds(KeepAlive * 1.5), 1);

                var _ = pingWatch.RunAsync(default);
            }

            _ = messageWorker.RunAsync(default);
        }

        protected virtual ValueTask<int> AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, Accepted }, cancellationToken);
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

        protected override void OnCompleted(Exception exception = null) { }

        protected override void OnConnect(byte header, ReadOnlySequence<byte> sequence)
        {
            throw new NotSupportedException();
        }

        protected override void OnPingReq(byte header, ReadOnlySequence<byte> sequence)
        {
            if(header != 0b1100_0000) throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "PINGREQ"));

            Post(PingRespPacket);
        }

        protected override void OnDisconnect(byte header, ReadOnlySequence<byte> sequence)
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
            GC.SuppressFinalize(this);

            await using(pingWatch.ConfigureAwait(false))
            await using(messageWorker.ConfigureAwait(false))
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