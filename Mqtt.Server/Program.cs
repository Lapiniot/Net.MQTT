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
            Console.WriteLine("Starting MQTT server...");
            var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName()).ConfigureAwait(false);

            var server = new MqttServer();

            var ipEndPoint = new IPEndPoint(addresses[0], 1883);
            Console.WriteLine($"Starting listener at tcp://{ipEndPoint}");
            server.RegisterListener("tcp.default", new TcpSocketListener(ipEndPoint));

            var uri = new Uri("http://localhost:8000/mqtt/");
            Console.WriteLine($"Starting listener at {uri}");
            server.RegisterListener("ws.default", new WebSocketsListener(uri, "mqtt", "mqttv3.1"));

            server.Start();
            Console.WriteLine("Started.");
            Console.WriteLine("Press any key to stop server...");
            Console.ReadKey();
        }
    }
}