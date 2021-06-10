using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Policies;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Channels.Channel;
using static System.Threading.Interlocked;
using static System.Threading.Tasks.TaskContinuationOptions;
using static System.TimeSpan;

namespace System.Net.Mqtt.Client
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Type implements IAsyncDisposable instead")]
    public sealed partial class MqttClient : MqttClientProtocol, ISessionStateRepository<MqttClientSessionState>, IConnectedObject
    {
        private const long StateConnected = 0;
        private const long StateDisconnected = 1;
        private const long StateAborted = 2;
        private readonly IRetryPolicy reconnectPolicy;
        private readonly ISessionStateRepository<MqttClientSessionState> repository;
        private readonly string clientId;
#pragma warning disable CA2213 // Disposable fields should be disposed: Warning is wrongly emitted due to some issues with analyzer itself
        private DelayWorkerLoop pinger;
#pragma warning disable CA2213
        private MqttClientSessionState sessionState;
        private long connectionState;

        public MqttClient(NetworkTransport transport, string clientId,
            ISessionStateRepository<MqttClientSessionState> repository = null,
            IRetryPolicy reconnectPolicy = null) :
            base(transport)
        {
            this.clientId = clientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext());
            this.repository = repository ?? this;
            this.reconnectPolicy = reconnectPolicy;

            (incomingQueueReader, incomingQueueWriter) = CreateUnbounded<MqttMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
            dispatcher = new WorkerLoop(DispatchMessageAsync);
            publishObservers = new ObserversContainer<MqttMessage>();
            pendingCompletions = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();
        }

        public MqttClient(NetworkTransport transport) : this(transport, null) { }

        public string ClientId => clientId;

        public bool CleanSession { get; private set; }

        public event EventHandler<ConnectedEventArgs> Connected;

        public event EventHandler<DisconnectedEventArgs> Disconnected;

        protected override void OnPacketReceived() { }

        protected override void OnConnAck(byte header, ReadOnlySequence<byte> reminder) { }

        protected override async Task StartingAsync(object state, CancellationToken cancellationToken)
        {
            await Transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var options = (state as MqttConnectionOptions) ?? new MqttConnectionOptions();

            var cleanSession = Read(ref connectionState) != StateAborted && options.CleanSession;

            var connectPacket = new ConnectPacket(clientId, 0x04, "MQTT", options.KeepAlive, cleanSession,
                options.UserName, options.Password, options.LastWillTopic, options.LastWillMessage,
                options.LastWillQoS, options.LastWillRetain);

            var buffer = new byte[connectPacket.GetSize(out var remainingLength)];
            connectPacket.Write(buffer, remainingLength);
            await Transport.SendAsync(buffer, cancellationToken).ConfigureAwait(false);

            var rt = ReadPacketAsync(cancellationToken);

            var sequence = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

            if(!ConnAckPacket.TryRead(sequence, out var packet))
            {
                throw new InvalidDataException(InvalidConnAckPacket);
            }

            packet.EnsureSuccessStatusCode();

            CleanSession = !packet.SessionPresent;

            sessionState = repository.GetOrCreate(clientId, CleanSession, out _);

            if(CleanSession)
            {
                // discard all not delivered application level messages
                while(incomingQueueReader.TryRead(out _)) { }
            }
            else
            {
                Parallel.ForEach(sessionState.ResendPackets, Post);
            }

            await base.StartingAsync(null, cancellationToken).ConfigureAwait(false);

            _ = dispatcher.RunAsync(default);

            if(options.KeepAlive > 0)
            {
                pinger = new DelayWorkerLoop(PingAsync, FromSeconds(options.KeepAlive));
                _ = pinger.RunAsync(default);
            }

            connectionState = StateConnected;

            Connected?.Invoke(this, ConnectedEventArgs.GetInstance(CleanSession));
        }

        protected override async Task StoppingAsync()
        {
            Parallel.ForEach(pendingCompletions.Values, CancelCompletion);
            pendingCompletions.Clear();

            if(pinger != null)
            {
                await using(pinger.ConfigureAwait(false))
                {
                    await pinger.StopAsync().ConfigureAwait(false);
                }

                pinger = null;
            }

            await dispatcher.StopAsync().ConfigureAwait(false);

            await base.StoppingAsync().ConfigureAwait(false);

            var graceful = CompareExchange(ref connectionState, StateDisconnected, StateConnected) == StateConnected;

            if(graceful)
            {
                if(CleanSession) repository.Remove(clientId);

                await Transport.SendAsync(new byte[] { 0b1110_0000, 0 }, default).ConfigureAwait(false);
            }

            await Transport.DisconnectAsync().ConfigureAwait(false);

            if(graceful)
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs(false, false));
            }
        }

        private void CancelCompletion(TaskCompletionSource<object> source)
        {
            source.TrySetCanceled();
        }

        protected override bool OnCompleted(Exception exception = null)
        {
            if(exception != null && CompareExchange(ref connectionState, StateAborted, StateConnected) == StateConnected)
            {
                StopActivityAsync().ContinueWith(t =>
                {
                    var args = new DisconnectedEventArgs(true, reconnectPolicy != null);

                    Disconnected?.Invoke(this, args);

                    if(args.TryReconnect)
                    {
                        reconnectPolicy?.RetryAsync(async token =>
                        {
                            await StartActivityAsync(token).ConfigureAwait(false);
                            return true;
                        });
                    }
                }, default, RunContinuationsAsynchronously, TaskScheduler.Default);
            }

            return true;
        }

        public override async ValueTask DisposeAsync()
        {
            using(sessionState)
            using(publishObservers)
            await using(dispatcher.ConfigureAwait(false))
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task ConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken = default)
        {
            return this.StartActivityAsync(cancellationToken, options);
        }

        #region Implementation of ISessionStateRepository<out SessionState>

        public MqttClientSessionState GetOrCreate(string clientId, bool cleanSession, out bool existingSession)
        {
            if(cleanSession) Remove(clientId);
            existingSession = sessionState != null;
            return sessionState ?? new MqttClientSessionState();
        }

        public void Remove(string clientId)
        {
            sessionState?.Dispose();
            sessionState = null;
        }

        #endregion

        #region Implementation of IConnectedObject

        public bool IsConnected => IsRunning;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return StartActivityAsync(cancellationToken, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task DisconnectAsync()
        {
            return StopActivityAsync();
        }

        #endregion
    }
}