using System.Diagnostics;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Net.Sockets.SocketFlags;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : NetworkStreamParser<MqttConnectionOptions>
    {
        private readonly IIdentityPool<ushort> idPool = new FastIdentityPool(1);
        private CancellationTokenSource pingCancellationSource;

        public MqttClient(IPEndPoint endpoint, string clientId) : base(endpoint)
        {
            ClientId = clientId;
        }

        public MqttClient(IPEndPoint endpoint) :
            this(endpoint, Path.GetRandomFileName())
        {
        }

        public MqttConnectionOptions Options { get; private set; }

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

            await Socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);

            var buffer = new byte[4];
            var received = await Socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
            new ConnAckPacket(buffer.AsSpan(0, received)).EnsureSuccessStatusCode();
        }

        private async Task MqttDisconnectAsync()
        {
            await Socket.SendAsync(new byte[] {(byte)Disconnect, 0}, None, default).ConfigureAwait(false);
        }

        private Task MqttSendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            return MqttSendBytesAsync(packet.GetBytes(), cancellationToken);
        }

        private async Task MqttSendBytesAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
        {
            try
            {
                await Socket.SendAsync(bytes, None, cancellationToken).ConfigureAwait(false);
                ArisePingTask();
            }
            catch(SocketException se) when(se.SocketErrorCode == SocketError.ConnectionAborted)
            {
            }
        }

        private void ArisePingTask()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            var cts = Interlocked.Exchange(ref pingCancellationSource, tokenSource);
            if(cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            MqttPingAsync(Options.KeepAlive * 1000, token).ContinueWith(t =>
            {
                if(t.Exception != null)
                {
                    Trace.TraceError("Ping request error: " + t.Exception.Message);
                }
            });
        }

        private async Task MqttPingAsync(int millisecondsDelay, CancellationToken token)
        {
            await Task.Delay(millisecondsDelay, token).ConfigureAwait(false);
            await MqttSendBytesAsync(new byte[] {(byte)PingRec, 0}, token).ConfigureAwait(false);
        }

        #region Overrides of NetworkStreamParser<MqttConnectionOptions>

        protected override async Task OnConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(options, cancellationToken).ConfigureAwait(false);
            await MqttConnectAsync(options, cancellationToken).ConfigureAwait(false);
            Options = options;
        }

        protected override async Task OnCloseAsync()
        {
            try
            {
                var tcs = pingCancellationSource;

                if(tcs != null)
                {
                    tcs.Cancel();
                    tcs.Dispose();
                }

                await MqttDisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            await base.OnCloseAsync().ConfigureAwait(false);
        }
    }

    #endregion
}