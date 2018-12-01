using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
using System.Net.Transports;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Tasks.TaskContinuationOptions;
using static System.TimeSpan;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : MqttClientProtocol
    {
        private const long StateConnected = 0;
        private const long StateDisconnected = 1;
        private const long StateAborted = 2;
        private readonly DelayWorkerLoop<object> pingWorker;
        private readonly IRetryPolicy reconnectPolicy;
        private long connectionState;

        public MqttClient(INetworkTransport transport, string clientId,
            MqttConnectionOptions options = null, IRetryPolicy reconnectPolicy = null) :
            base(transport, new NetworkPipeReader(transport))
        {
            ClientId = clientId;
            ConnectionOptions = options ?? new MqttConnectionOptions();
            this.reconnectPolicy = reconnectPolicy;

            idPool = new FastPacketIdPool();

            var channel = Channel.CreateUnbounded<MqttMessage>();
            messageQueueReader = channel.Reader;
            messageQueueWriter = channel.Writer;
            messageDispatcher = new WorkerLoop<object>(DispatchMessageAsync, null);

            publishObservers = new ObserversContainer<MqttMessage>();

            receiveFlowPackets = new Dictionary<ushort, MqttPacket>();
            publishFlowPackets = new HashQueue<ushort, MqttPacket>();
            pendingCompletions = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();

            pingWorker = new DelayWorkerLoop<object>(PingAsync, null, FromSeconds(ConnectionOptions.KeepAlive));
        }

        public MqttClient(NetworkTransport transport) : this(transport, Path.GetRandomFileName()) {}

        public MqttConnectionOptions ConnectionOptions { get; }

        public string ClientId { get; }

        public bool CleanSession { get; private set; }

        protected override void Dispose(bool disposing)
        {
            using(Transport) {}

            using(Reader) {}

            using(publishObservers) {}

            using(publishFlowPackets) {}

            using(pingWorker) {}

            using(messageDispatcher) {}
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await Transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            await Reader.ConnectAsync(cancellationToken).ConfigureAwait(false);

            Reader.OnWriterCompleted(OnStreamCompleted, null);

            var co = ConnectionOptions;

            var cleanSession = Interlocked.Read(ref connectionState) != StateAborted && co.CleanSession;

            var connectPacket = new ConnectPacketV4(ClientId, co.KeepAlive, cleanSession,
                co.UserName, co.Password, co.LastWillTopic, co.LastWillMessage,
                co.LastWillQoS, co.LastWillRetain);

            await SendAsync(connectPacket.GetBytes(), cancellationToken).ConfigureAwait(false);

            var rt = ReadPacketAsync(cancellationToken);

            var sequence = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

            if(!ConnAckPacket.TryParse(sequence, out var packet))
            {
                throw new InvalidDataException(InvalidConnAckPacket);
            }

            packet.EnsureSuccessStatusCode();

            CleanSession = !packet.SessionPresent;

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

            messageDispatcher.Start();

            pingWorker.Start();

            connectionState = StateConnected;

            OnConnected(new ConnectedEventArgs(CleanSession));

            foreach(var mqttPacket in publishFlowPackets) Post(mqttPacket.GetBytes());
        }

        protected override async Task OnDisconnectAsync()
        {
            await messageDispatcher.StopAsync().ConfigureAwait(false);

            pingWorker.Stop();

            var isGracefulDisconnection = Interlocked.CompareExchange(ref connectionState, StateDisconnected, 0) == 0;

            if(isGracefulDisconnection)
            {
                // We are the first here who set connectionState = 1, this means graceful disconnection by user code.
                // Extra actions to do:
                // 1. MQTT DISCONNECT message must be sent
                // 2. Raise Disconnected event with args.Aborted == false to notify normal disconnection

                try
                {
                    await SendAsync(new byte[] {(byte)Disconnect, 0}, default).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                await Task.WhenAll(
                    base.OnDisconnectAsync(),
                    Reader.DisconnectAsync(),
                    Transport.DisconnectAsync()).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            if(isGracefulDisconnection)
            {
                OnDisconnected(new DisconnectedEventArgs(false, false));
            }
        }

        public event ConnectedEventHandler Connected;

        public event DisconnectedEventHandler Disconnected;

        protected virtual void OnConnected(ConnectedEventArgs args)
        {
            Connected?.Invoke(this, args);
        }

        protected virtual void OnDisconnected(DisconnectedEventArgs args)
        {
            Disconnected?.Invoke(this, args);
        }

        protected override void OnPacketReceived() {}

        protected override void OnConAck(byte header, ReadOnlySequence<byte> remainder) {}

        private void OnStreamCompleted(Exception exception, object state)
        {
            if(exception != null)
            {
                if(Interlocked.CompareExchange(ref connectionState, StateAborted, 0) == 0)
                {
                    DisconnectAsync().ContinueWith(t =>
                    {
                        var args = new DisconnectedEventArgs(true, reconnectPolicy != null);

                        OnDisconnected(args);

                        if(args.TryReconnect)
                        {
                            reconnectPolicy?.RetryAsync(ConnectAsync);
                        }
                    }, RunContinuationsAsynchronously);
                }
            }
        }
    }
}