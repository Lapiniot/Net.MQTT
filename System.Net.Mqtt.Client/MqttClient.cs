using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Transports;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : NetworkStreamParser
    {
        private const long StateConnected = 0;
        private const long StateDisconnected = 1;
        private const long StateAborted = 2;
        private readonly IRetryPolicy reconnectPolicy;

        private readonly NetworkTransport transport;

        private long connectionState;

        public MqttClient(NetworkTransport transport, string clientId,
            MqttConnectionOptions options = null,
            IRetryPolicy reconnectPolicy = null) : base(transport)
        {
            this.transport = transport;
            this.reconnectPolicy = reconnectPolicy;
            ClientId = clientId;
            dispatchQueue = new AsyncBlockingQueue<MqttMessage>();
            publishObservers = new ObserversContainer<MqttMessage>();
            receiveFlowPackets = new Dictionary<ushort, MqttPacket>();
            publishFlowPackets = new HashQueue<ushort, MqttPacket>();
            idPool = new FastIdentityPool(1);
            pendingCompletions = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();
            ConnectionOptions = options ?? new MqttConnectionOptions();
        }

        public MqttClient(NetworkTransport transport) : this(transport, Path.GetRandomFileName())
        {
        }

        public MqttConnectionOptions ConnectionOptions { get; }

        public string ClientId { get; }

        public bool CleanSession { get; private set; }

        protected override void Dispose(bool disposing)
        {
            using(transport)
            using(publishObservers)
            using(publishFlowPackets)
            using(dispatchQueue)
            {
            }
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var cleanSession = Interlocked.Read(ref connectionState) != StateAborted && ConnectionOptions.CleanSession;

            var connectPacket = new ConnectPacket(ClientId)
            {
                ProtocolName = "MQTT",
                ProtocolLevel = 0x04,
                KeepAlive = ConnectionOptions.KeepAlive,
                CleanSession = cleanSession,
                UserName = ConnectionOptions.UserName,
                Password = ConnectionOptions.Password,
                WillTopic = ConnectionOptions.LastWillTopic,
                WillMessage = ConnectionOptions.LastWillMessage,
                WillQoS = ConnectionOptions.LastWillQoS,
                WillRetain = ConnectionOptions.LastWillRetain
            };

            CleanSession = !await MqttConnectAsync(cancellationToken, connectPacket).ConfigureAwait(false);

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            StartDispatcher();

            StartPingWorker();

            connectionState = StateConnected;

            OnConnected(new ConnectedEventArgs(CleanSession));

            var unused = Task.Run(ResendPublishPacketsAsync);

            return Task.CompletedTask;
        }

        protected override async Task OnDisconnectAsync()
        {
            await StopDispatcherAsync().ConfigureAwait(false);

            await StopPingWorkerAsync().ConfigureAwait(false);

            var isGracefulDisconnection = Interlocked.CompareExchange(ref connectionState, StateDisconnected, 0) == 0;

            if(isGracefulDisconnection)
            {
                // We are the first here who set connectionState = 1, this means graceful disconnection by user code.
                // Extra actions to do:
                // 1. MQTT DISCONNECT message must be sent
                // 2. Raise Disconnected event with args.Aborted == false to notify normal disconnection

                try
                {
                    await MqttDisconnectAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                await base.OnDisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            try
            {
                await transport.DisconnectAsync().ConfigureAwait(false);
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

        protected override void OnEndOfStream()
        {
            OnConnectionAborted();
        }

        protected override void OnConnectionAborted()
        {
            if(Interlocked.CompareExchange(ref connectionState, StateAborted, 0) == 0)
            {
                DisconnectAsync().ContinueWith(t =>
                {
                    var args = new DisconnectedEventArgs(true, true);

                    OnDisconnected(args);

                    if(args.TryReconnect)
                    {
                        reconnectPolicy?.RetryAsync(ConnectAsync);
                    }
                }, RunContinuationsAsynchronously);
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
    }
}