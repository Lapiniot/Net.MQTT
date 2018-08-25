using System.Common;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public class MqttClient : AsyncConnectedObject<MqttConnectionOptions>
    {
        private Socket socket;
        public string ClientId { get; }
        public IPEndPoint Endpoint { get; }
        public MqttClient(IPEndPoint endpoint, string clientId)
        {
            Endpoint = endpoint;
            ClientId = clientId;
        }

        public MqttClient(IPEndPoint endpoint) :
            this(endpoint, Path.GetRandomFileName())
        {
        }

        protected override async Task OnConnectAsync(MqttConnectionOptions options)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(Endpoint);
            TryConnect();
        }

        private void TryConnect()
        {
            Span<byte> buffer = stackalloc byte[100];
            buffer[0] = (byte)PacketType.Connect;
            socket.Send(buffer);
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