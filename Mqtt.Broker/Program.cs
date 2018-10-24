using System;
using System.Net;
using System.Net.Listeners;
using System.Net.Mqtt.Broker;
using System.Threading.Tasks;

namespace Mqtt.Broker
{
    internal class Program
    {
        private static async Task Main()
        {
            var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName()).ConfigureAwait(false);

            var broker = new MqttBroker();

            broker.AddListener("tcp.default", new TcpSocketConnectionListener(new IPEndPoint(addresses[0], 1883)));
            broker.AddListener("ws.default", new WebSocketsConnectionListener(new Uri("http://localhost:8000/mqtt/"), "mqtt", "mqttv3.1"));

            broker.Start();

            Console.ReadKey();
        }
    }
}