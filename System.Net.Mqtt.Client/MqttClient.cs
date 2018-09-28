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

        private async Task MqttConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            var message = new ConnectPacket(ClientId)
            {
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
            new ConnAckPacket(buffer.AsSpan(0, received)).EnsureSuccessStatusCode();
        }

        private async Task MqttDisconnectAsync()
        {
            await SendAsync(new byte[] {(byte)Disconnect, 0}, default).ConfigureAwait(false);
        }

        private Task MqttSendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            return MqttSendBytesAsync(packet.GetBytes(), cancellationToken);
        }

        private async Task MqttSendBytesAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
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
            base.Dispose(disposing);

            publishObservers.Dispose();

            publishFlowPackets.Dispose();

            dispatchQueue.Dispose();
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);
            await MqttConnectAsync(ConnectionOptions, cancellationToken).ConfigureAwait(false);
            aborted = 0;
        }

        protected override async Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectedAsync(cancellationToken).ConfigureAwait(false);

            StartDispatcher();

            StartPingWorker();
        }

        protected override async Task OnDisconnectAsync()
        {
            try
            {
                await StopDispatcherAsync().ConfigureAwait(false);

                await StopPingWorkerAsync().ConfigureAwait(false);

                // Prevent ConnectionAborted event firing in case of graceful termination
                aborted = 1;

                await MqttDisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }


            await base.OnDisconnectAsync().ConfigureAwait(false);
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
                    NotifyConnectionAborted();
                    reconnectPolicy?.RetryAsync(ConnectAsync, default);
                }, RunContinuationsAsynchronously);
            }
        }
    }
}