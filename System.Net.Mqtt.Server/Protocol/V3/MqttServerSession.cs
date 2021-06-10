using System.Buffers;
using System.IO;
using System.Net.Connections.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Packets.ConnAckPacket;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class MqttServerSession : Server.MqttServerSession
    {
        private static readonly byte[] PingRespPacket = new byte[] { 0b1101_0000, 0b0000_0000 };
        private readonly ISessionStateRepository<MqttServerSessionState> repository;
        private readonly IObserver<SubscriptionRequest> subscribeObserver;
#pragma warning disable CA2213 // Disposable fields should be disposed: Warning is wrongly emitted due to some issues with analyzer itself
        private readonly WorkerLoop messageWorker;
        private DelayWorkerLoop pingWatch;
#pragma warning restore CA2213
#pragma warning disable CA2213 // Disposable fields should be disposed: Irrelevant warning as soon as we do not take ownership on this instance
        private MqttServerSessionState sessionState;
#pragma warning disable CA2213

        public MqttServerSession(string clientId, NetworkTransport transport,
            ISessionStateRepository<MqttServerSessionState> stateRepository,
            ILogger logger, IObserver<SubscriptionRequest> subscribeObserver,
            IObserver<MessageRequest> messageObserver) :
            base(clientId, transport, logger, messageObserver)
        {
            repository = stateRepository;
            this.subscribeObserver = subscribeObserver;
            messageWorker = new WorkerLoop(ProcessMessageAsync);
        }

        public bool CleanSession { get; init; }
        public ushort KeepAlive { get; init; }
        public Message WillMessage { get; init; }

        protected override void OnPacketSent() { }

        protected override async Task StartingAsync(object state, CancellationToken cancellationToken)
        {
            sessionState = repository.GetOrCreate(ClientId, CleanSession, out var existing);

            await AcknowledgeConnection(existing, cancellationToken).ConfigureAwait(false);

            foreach(var packet in sessionState.ResendPackets) Post(packet);

            await base.StartingAsync(state, cancellationToken).ConfigureAwait(false);

            sessionState.IsActive = true;

            sessionState.WillMessage = WillMessage;

            if(KeepAlive > 0)
            {
                pingWatch = new DelayWorkerLoop(NoPingDisconnectAsync, TimeSpan.FromSeconds(KeepAlive * 1.5), 1);

                var _ = pingWatch.RunAsync(default);
            }

            _ = messageWorker.RunAsync(default);
        }

        protected virtual ValueTask AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, Accepted }, cancellationToken);
        }

        protected override async Task StoppingAsync()
        {
            try
            {
                if(sessionState.WillMessage != null)
                {
                    OnMessageReceived(sessionState.WillMessage);
                    sessionState.WillMessage = null;
                }

                if(pingWatch != null)
                {
                    await pingWatch.StopAsync().ConfigureAwait(false);
                }

                await messageWorker.StopAsync().ConfigureAwait(false);

                await base.StoppingAsync().ConfigureAwait(false);
            }
            catch(ConnectionAbortedException)
            {
                // Expected here - shouldn't cause exception during termination even 
                // if connection was aborted before due to any reasons
            }
            finally
            {
                if(CleanSession)
                {
                    repository.Remove(ClientId);
                }
                else
                {
                    sessionState.IsActive = false;
                }
            }
        }

        protected override bool OnCompleted(Exception exception = null)
        {
            // suppress ConnectionAbortedException exception if client sent DISCONNECT 
            // message and then terminated connection by itself
            return exception is ConnectionAbortedException && DisconnectReceived;
        }

        protected override void OnConnect(byte header, ReadOnlySequence<byte> reminder)
        {
            throw new NotSupportedException();
        }

        protected override void OnPingReq(byte header, ReadOnlySequence<byte> reminder)
        {
            Post(PingRespPacket);
        }

        protected override void OnDisconnect(byte header, ReadOnlySequence<byte> reminder)
        {
            // Graceful disconnection: no need to dispatch last will message
            sessionState.WillMessage = null;

            DisconnectReceived = true;

            _ = StopAsync();
        }

        private Task NoPingDisconnectAsync(CancellationToken cancellationToken)
        {
            _ = StopAsync();
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
    }
}