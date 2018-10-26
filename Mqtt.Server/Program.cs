using System;
using System.Net;
using System.Net.Listeners;
using System.Net.Mqtt.Server;
using System.Threading.Tasks;

namespace Mqtt.Server
{
    internal class Program
    {
        private static async Task Main()
        {
            var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName()).ConfigureAwait(false);

            var server = new MqttServer();

            server.AddListener("tcp.default", new TcpSocketConnectionListener(new IPEndPoint(addresses[0], 1883)));
            server.AddListener("ws.default", new WebSocketsConnectionListener(new Uri("http://localhost:8000/mqtt/"), "mqtt", "mqttv3.1"));

            server.Start();

            Console.ReadKey();
        }
    }
}