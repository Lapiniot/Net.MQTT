using System.IO;
using System.Net.Mqtt.Client.Exceptions;
using System.Net.Mqtt.Messages;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(Endpoint);
            var message = new ConnectMessage(ClientId)
            {
                KeepAlive = 60,
                CleanSession = options.CleanSession,
                UserName = options.UserName,
                Password = options.Password,
                LastWillTopic = options.LastWillTopic,
                LastWillMessage = options.LastWillMessage,
                LastWillQoS = options.LastWillQoS,
                LastWillRetain = options.LastWillRetain
            };

            await socket.SendAsync(message.GetBytes(), SocketFlags.None, cancellationToken).ConfigureAwait(false);

            var buffer = new byte[4];
            var count = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            if(count != 4 || buffer[0] != (byte)PacketType.ConnAck || buffer[1] != 2)
            {
                throw new InvalidOperationException("Invalid CONNECT response. Valid CONNACK packet expected.");
            }
            switch(buffer[3])
            {
                case 0: return;
                case 1: throw new MqttInvalidProtocolVersionException();
                case 2: throw new MqttInvalidIdentifierException();
                case 3: throw new MqttServerUnavailableException();
                case 4: throw new MqttInvalidUserCredentialsException();
                case 5: throw new MqttNotAuthorizedException();
            }
        }

        protected override Task OnCloseAsync()
        {
            socket.DisconnectAsync(new SocketAsyncEventArgs());
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            return Task.CompletedTask;
        }
    }
}