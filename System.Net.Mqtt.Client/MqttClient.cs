using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Transports;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : NetworkStreamParser
    {
        private const long StateConnected = 0;
        private const long StateDisconnected = 1;
        private const long StateAborted = 2;
        private readonly IIdentityPool idPool;

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;
        private readonly IRetryPolicy reconnectPolicy;

        private long connectionState;

        public MqttClient(NetworkTransport transport, string clientId,
            MqttConnectionOptions options = null, bool disposeTransport = true,
            IRetryPolicy reconnectPolicy = null) :
            base(transport, disposeTransport)
        {
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

        public MqttClient(NetworkTransport transport, bool disposeTransport = true) :
            this(transport, Path.GetRandomFileName(), null, disposeTransport)
        {
        }

        public MqttConnectionOptions ConnectionOptions { get; }

        public string ClientId { get; }

        public bool CleanSession { get; private set; }

        private async Task<bool> MqttConnectAsync(CancellationToken cancellationToken, ConnectPacket connectPacket)
        {
            await SendAsync(connectPacket.GetBytes(), cancellationToken).ConfigureAwait(false);

            var buffer = new byte[4];

            var received = await ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            var packet = new ConnAckPacket(buffer.AsSpan(0, received));

            packet.EnsureSuccessStatusCode();

            return packet.SessionPresent;
        }

        private async Task MqttDisconnectAsync()
        {
            await SendAsync(new byte[] {(byte)Disconnect, 0}, default).ConfigureAwait(false);
        }

        private Task MqttSendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            return MqttSendPacketAsync(packet.GetBytes(), cancellationToken);
        }

        private async Task MqttSendPacketAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
        {
            await SendAsync(bytes, cancellationToken).ConfigureAwait(false);

            ArisePingTimer();
        }

        private async Task<T> PostPacketAsync<T>(MqttPacketWithId packet, CancellationToken cancellationToken) where T : class
        {
            var packetId = packet.Id;

            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            pendingCompletions.TryAdd(packetId, completionSource);

            try
            {
                await MqttSendPacketAsync(packet, cancellationToken).ConfigureAwait(false);

                return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as T;
            }
            finally
            {
                pendingCompletions.TryRemove(packetId, out _);
                idPool.Return(packetId);
            }
        }

        private void AcknowledgePacket(ushort packetId, object result = null)
        {
            if(pendingCompletions.TryGetValue(packetId, out var tcs))
            {
                tcs.TrySetResult(result);
            }
        }

        protected override void Dispose(bool disposing)
        {
            using(publishObservers)
            using(publishFlowPackets)
            using(dispatchQueue)
            {
                base.Dispose(disposing);
            }
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

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
        }

        protected override async Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectedAsync(cancellationToken).ConfigureAwait(false);

            StartDispatcher();

            StartPingWorker();

            connectionState = StateConnected;

            OnConnected(new ConnectedEventArgs(CleanSession));

            var unused = Task.Run(ResendPublishPacketsAsync);
        }

        private async Task ResendPublishPacketsAsync()
        {
            foreach(var tuple in publishFlowPackets)
            {
                await MqttSendPacketAsync(tuple.Value).ConfigureAwait(true);
            }
        }

        protected override async Task OnDisconnectAsync()
        {
            await StopDispatcherAsync().ConfigureAwait(false);

            await StopPingWorkerAsync().ConfigureAwait(false);

            if(Interlocked.CompareExchange(ref connectionState, StateDisconnected, 0) == 0)
            {
                // We are the first here who set connectionState = 1, this means graceful disconnection by user code.
                // 1. MQTT DISCONNECT message must be sent
                // 2. base.OnDisconnectAsync() to be called
                // 3. raise Disconnected event with args.Aborted == false to notify normal disconnection

                try
                {
                    await MqttDisconnectAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }

                await base.OnDisconnectAsync().ConfigureAwait(false);

                OnDisconnected(new DisconnectedEventArgs(false, false));
            }
            else
            {
                // Connection to the server is already broken, just calling base.OnDisconnectAsync()
                // to perform essential cleanup
                await base.OnDisconnectAsync().ConfigureAwait(false);
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