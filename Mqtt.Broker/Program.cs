using System;
using System.Net;
using System.Net.Mqtt.Broker;
using System.Threading.Tasks;

namespace Mqtt.Broker
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName()).ConfigureAwait(false);

            var broker = new MqttBroker(new IPEndPoint(addresses[0], 1883));

            broker.Start();
        }
    }
}