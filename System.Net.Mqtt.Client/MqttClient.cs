using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Mqtt.Messages;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Sockets.SocketFlags;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : NetworkStreamParser<MqttConnectionOptions>
    {
        private readonly IIdentityPool<ushort> idPool = new FastIdentityPool(1);

        public MqttClient(IPEndPoint endpoint, string clientId) : base(endpoint)
        {
            ClientId = clientId;
        }

        public MqttClient(IPEndPoint endpoint) :
            this(endpoint, Path.GetRandomFileName())
        {
        }

        public string ClientId { get; }

        private async Task MqttConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            var message = new ConnectMessage(ClientId)
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
            new ConnAckMessage(buffer.AsSpan(0, received)).EnsureSuccessStatusCode();
        }

        private async Task MqttDisconnectAsync()
        {
            await Socket.SendAsync(new byte[] { (byte)PacketType.Disconnect, 0 }, None, default).ConfigureAwait(false);
        }

        private async Task MqttSendMessageAsync(MqttMessage message, CancellationToken cancellationToken = default)
        {
            await Socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);
        }

        #region Overrides of NetworkStreamParser<MqttConnectionOptions>

        protected override async Task OnConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(options, cancellationToken).ConfigureAwait(false);
            await MqttConnectAsync(options, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnCloseAsync()
        {
            try
            {
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