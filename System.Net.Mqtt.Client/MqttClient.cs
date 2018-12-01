using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Properties.Strings;
using static System.Threading.Interlocked;
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

            receivedQoS2 = new Dictionary<ushort, MqttPacket>();
            publishFlowPackets = new HashQueue<ushort, MqttPacket>();
            pendingCompletions = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();

            pingWorker = new DelayWorkerLoop<object>(PingAsync, null, FromSeconds(ConnectionOptions.KeepAlive));
        }

        public MqttClient(INetworkTransport transport) : this(transport, Path.GetRandomFileName()) {}

        public MqttConnectionOptions ConnectionOptions { get; }

        public string ClientId { get; }

        public bool CleanSession { get; private set; }

        public event ConnectedEventHandler Connected;

        public event DisconnectedEventHandler Disconnected;

        protected override void OnPacketReceived() {}

        protected override void OnConAck(byte header, ReadOnlySequence<byte> remainder) {}

        private void OnStreamCompleted(Exception exception, object state)
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
                }, RunContinuationsAsynchronously);
            }
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await Transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            await Reader.ConnectAsync(cancellationToken).ConfigureAwait(false);

            Reader.OnWriterCompleted(OnStreamCompleted, null);

            var co = ConnectionOptions;

            var cleanSession = Read(ref connectionState) != StateAborted && co.CleanSession;

            var connectPacket = new ConnectPacketV4(ClientId, co.KeepAlive, cleanSession,
                co.UserName, co.Password, co.LastWillTopic, co.LastWillMessage,
                co.LastWillQoS, co.LastWillRetain);

            await Transport.SendAsync(connectPacket.GetBytes(), cancellationToken).ConfigureAwait(false);

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

            Connected?.Invoke(this, new ConnectedEventArgs(CleanSession));

            foreach(var mqttPacket in publishFlowPackets) Post(mqttPacket.GetBytes());
        }

        protected override async Task OnDisconnectAsync()
        {
            pingWorker.Stop();

            messageDispatcher.Stop();

            await base.OnDisconnectAsync().ConfigureAwait(false);

            var graceful = CompareExchange(ref connectionState, StateDisconnected, StateConnected) == StateConnected;

            if(graceful)
            {
                await Transport.SendAsync((Memory<byte>)new byte[] {(byte)Disconnect, 0}, default).ConfigureAwait(false);
            }

            await Task.WhenAll(Transport.DisconnectAsync(), Reader.DisconnectAsync()).ConfigureAwait(false);

            if(graceful)
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs(false, false));
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            using(publishObservers) {}

            using(pingWorker) {}

            using(messageDispatcher) {}

            using(Transport) {}

            using(Reader) {}

            using(publishFlowPackets) {}
        }
    }
}