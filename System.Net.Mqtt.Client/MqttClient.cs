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
        private readonly IIdentityPool idPool;

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;
        private readonly IRetryPolicy reconnectPolicy;
        private bool cleanSession;

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

        private async Task<bool> MqttConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            var message = new ConnectPacket(ClientId)
            {
                ProtocolName = "MQTT",
                ProtocolLevel = 0x04,
                KeepAlive = options.KeepAlive,
                CleanSession = options.CleanSession,
                UserName = options.UserName,
                Password = options.Password,
                WillTopic = options.LastWillTopic,
                WillMessage = options.LastWillMessage,
                WillQoS = options.LastWillQoS,
                WillRetain = options.LastWillRetain
            };

            await SendAsync(message.GetBytes(), cancellationToken).ConfigureAwait(false);

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

            var options = Interlocked.Read(ref aborted) == 0
                ? ConnectionOptions
                : new MqttConnectionOptions
                {
                    CleanSession = false,
                    KeepAlive = ConnectionOptions.KeepAlive,
                    UserName = ConnectionOptions.UserName,
                    Password = ConnectionOptions.Password,
                    LastWillRetain = ConnectionOptions.LastWillRetain,
                    LastWillTopic = ConnectionOptions.LastWillTopic,
                    LastWillMessage = ConnectionOptions.LastWillMessage,
                    LastWillQoS = ConnectionOptions.LastWillQoS
                };

            cleanSession = !await MqttConnectAsync(options, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectedAsync(cancellationToken).ConfigureAwait(false);

            StartDispatcher();

            StartPingWorker();

            aborted = 0;

            OnConnected(new ConnectedEventArgs(cleanSession));
        }

        protected override async Task OnDisconnectAsync()
        {
            await StopDispatcherAsync().ConfigureAwait(false);

            await StopPingWorkerAsync().ConfigureAwait(false);

            if(Interlocked.CompareExchange(ref aborted, 1, 0) == 0)
            {
                // We are the first here who set aborted = 1, this means graceful disconnection by user code.
                // 1. MQTT DISCONNECT message must be sent
                // 2. base.OnDisconnectAsync() to be called
                // 3. aborted flag reset to 0 (client connection state restored to initial disconnected)
                try
                {
                    await MqttDisconnectAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }

                await base.OnDisconnectAsync().ConfigureAwait(false);

                aborted = 0;
            }
            else
            {
                // Connection to the broker is already broken, just calling base.OnDisconnectAsync()
                // to perform essential cleanup
                await base.OnDisconnectAsync().ConfigureAwait(false);
            }

            OnDisconnected(new DisconnectedEventArgs(false, false));
        }

        protected override void OnEndOfStream()
        {
            OnConnectionAborted();
        }

        protected override void OnConnectionAborted()
        {
            if(Interlocked.CompareExchange(ref aborted, 1, 0) == 0)
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