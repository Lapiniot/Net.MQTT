﻿using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Connections;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
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
    public partial class MqttClient : MqttClientProtocol<NetworkPipeProducer>, ISessionStateRepository<MqttClientSessionState>
    {
        private const long StateConnected = 0;
        private const long StateDisconnected = 1;
        private const long StateAborted = 2;
        private readonly DelayWorkerLoop<object> pingWorker;
        private readonly IRetryPolicy reconnectPolicy;
        private readonly ISessionStateRepository<MqttClientSessionState> repository;
        private long connectionState;
        private MqttClientSessionState sessionState;

        public MqttClient(INetworkConnection connection, string clientId, ISessionStateRepository<MqttClientSessionState> repository = null,
            MqttConnectionOptions options = null, IRetryPolicy reconnectPolicy = null) :
            base(connection, new NetworkPipeProducer(connection))
        {
            this.repository = repository ?? this;
            ClientId = clientId;
            ConnectionOptions = options ?? new MqttConnectionOptions();
            this.reconnectPolicy = reconnectPolicy;

            (incomingQueueReader, incomingQueueWriter) = CreateUnbounded<MqttMessage>(new UnboundedChannelOptions {SingleReader = true, SingleWriter = true});

            messageDispatcher = new WorkerLoop<object>(DispatchMessageAsync, null);

            publishObservers = new ObserversContainer<MqttMessage>();

            pendingCompletions = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();

            if(ConnectionOptions.KeepAlive > 0)
            {
                pingWorker = new DelayWorkerLoop<object>(PingAsync, null, FromSeconds(ConnectionOptions.KeepAlive));
            }
        }

        public MqttClient(INetworkConnection connection) : this(connection, Path.GetRandomFileName()) {}

        public MqttConnectionOptions ConnectionOptions { get; }

        public string ClientId { get; }

        public bool CleanSession { get; private set; }

        public event ConnectedEventHandler Connected;

        public event DisconnectedEventHandler Disconnected;

        protected override void OnPacketReceived() {}

        protected override void OnConnAck(byte header, ReadOnlySequence<byte> remainder) {}

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await Transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            await Reader.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var co = ConnectionOptions;

            var cleanSession = Read(ref connectionState) != StateAborted && co.CleanSession;

            var connectPacket = new ConnectPacket(ClientId, 0x04, "MQTT", co.KeepAlive, cleanSession,
                co.UserName, co.Password, co.LastWillTopic, co.LastWillMessage,
                co.LastWillQoS, co.LastWillRetain);

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

            sessionState = repository.GetOrCreate(ClientId, CleanSession, out _);

            if(CleanSession)
            {
                // discard all not delivered application level messages
                await foreach(var _ in incomingQueueReader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {}
            }
            else
            {
                foreach(var mqttPacket in sessionState.GetResendPackets()) Post(mqttPacket);
            }

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

            messageDispatcher.Start();
            pingWorker?.Start();

            connectionState = StateConnected;
            Connected?.Invoke(this, new ConnectedEventArgs(CleanSession));
        }

        protected override async Task OnDisconnectAsync()
        {
            foreach(var source in pendingCompletions.Values) source.TrySetCanceled();
            pendingCompletions.Clear();

            pingWorker?.Stop();

            messageDispatcher.Stop();

            await base.OnDisconnectAsync().ConfigureAwait(false);

            var graceful = CompareExchange(ref connectionState, StateDisconnected, StateConnected) == StateConnected;

            if(graceful)
            {
                if(CleanSession) repository.Remove(ClientId);

                await Transport.SendAsync(new byte[] {0b1110_0000, 0}, default).ConfigureAwait(false);
            }

            await Task.WhenAll(Transport.DisconnectAsync(), Reader.DisconnectAsync()).ConfigureAwait(false);

            if(graceful)
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs(false, false));
            }
        }

        protected override void OnCompleted(Exception exception = null)
        {
            if(exception != null && CompareExchange(ref connectionState, StateAborted, StateConnected) == StateConnected)
            {
                DisconnectAsync().ContinueWith(t =>
                {
                    var args = new DisconnectedEventArgs(true, reconnectPolicy != null);

                    Disconnected?.Invoke(this, args);

                    if(args.TryReconnect)
                    {
                        reconnectPolicy?.RetryAsync(ConnectAsync);
                    }
                }, default, RunContinuationsAsynchronously, TaskScheduler.Default);
            }
        }

        public override async ValueTask DisposeAsync()
        {
            using(sessionState)
            await using(Reader.ConfigureAwait(false))
            await using(Transport.ConfigureAwait(false))
            await using(messageDispatcher.ConfigureAwait(false))
            await using(pingWorker.ConfigureAwait(false))
            using(publishObservers)
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
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
    }
}