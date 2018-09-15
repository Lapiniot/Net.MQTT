using System.Collections.Concurrent;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Sockets;
using System.Net.Transports;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : NetworkStreamParser
    {
        private readonly IIdentityPool<ushort> idPool = new FastIdentityPool(1);

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions =
            new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();

        public MqttClient(NetworkTransport transport, string clientId, MqttConnectionOptions options = null) : base(transport)
        {
            ClientId = clientId;
            ConnectionOptions = options ?? new MqttConnectionOptions();
        }

        public MqttClient(NetworkTransport transport) : this(transport, Path.GetRandomFileName())
        {
        }

        public MqttConnectionOptions ConnectionOptions { get; private set; }
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
            await SendAsync(new byte[] { (byte)Disconnect, 0 }, default).ConfigureAwait(false);
        }

        private Task MqttSendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            return MqttSendBytesAsync(packet.GetBytes(), cancellationToken);
        }

        private async Task MqttSendBytesAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
        {
            try
            {
                await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
                ArisePingTimer();
            }
            catch(SocketException se) when(se.SocketErrorCode == SocketError.ConnectionAborted)
            {
                OnConnectionAborted();

                throw;
            }
        }

        private async Task<T> PostMessageWithAcknowledgeAsync<T>(MqttPacketWithId packet, CancellationToken cancellationToken) where T : class
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

        #region Overrides of AsyncConnectedObject<MqttConnectionOptions>

        protected override void Dispose(bool disposing)
        {
            publishObservers?.Dispose();
            publishObservers = null;

            base.Dispose(disposing);
        }

        #endregion

        #region Overrides of NetworkStreamParser<MqttConnectionOptions>

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);
            StartDispatcher();
            await MqttConnectAsync(ConnectionOptions, cancellationToken).ConfigureAwait(false);
            StartPingWorker();
        }

        protected override async Task OnCloseAsync()
        {
            try
            {
                await StopDispatchAsync().ConfigureAwait(false);

                await StopPingWorkerAsync().ConfigureAwait(false);

                // Prevent ConnectionAborted event firing in case of graceful termination
                aborted = 1;

                await MqttDisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }


            await base.OnCloseAsync().ConfigureAwait(false);
        }

        protected override void OnEndOfStream()
        {
            OnConnectionAborted();
        }

        #endregion
    }
}