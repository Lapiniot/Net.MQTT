using System.IO;
using System.Net.Mqtt.Messages;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketFlags;

namespace System.Net.Mqtt.Client
{
    public class MqttClient : AsyncConnectedObject<MqttConnectionOptions>
    {
        private Socket socket;

        public MqttClient(IPEndPoint endpoint, string clientId)
        {
            Endpoint = endpoint;
            ClientId = clientId;
        }

        public MqttClient(IPEndPoint endpoint) :
            this(endpoint, Path.GetRandomFileName())
        {
        }

        public string ClientId { get; }

        public IPEndPoint Endpoint { get; }

        protected override async Task OnConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken)
        {
            socket = new Socket(InterNetwork, SocketType.Stream, Tcp);
            await socket.ConnectAsync(Endpoint);

            var message = new ConnectMessage(ClientId)
            {
                KeepAlive = options.KeepAlive,
                CleanSession = options.CleanSession,
                UserName = options.UserName,
                Password = options.Password,
                LastWillTopic = options.LastWillTopic,
                LastWillMessage = options.LastWillMessage,
                LastWillQoS = options.LastWillQoS,
                LastWillRetain = options.LastWillRetain
            };

            await socket.SendAsync(message.GetBytes(), None, cancellationToken).ConfigureAwait(false);

            var buffer = new byte[4];
            var received = await socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
            new ConnAckMessage(buffer.AsSpan(0, received)).EnsureSuccessStatusCode();
        }

        protected override async Task OnCloseAsync()
        {
            await socket.SendAsync(new byte[] { (byte)PacketType.Disconnect, 0 }, None, default);
            socket.Disconnect(false);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}