using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Client;
using System.Net.Transports;
using System.Policies;
using System.Text;
using System.Threading.Tasks;

namespace Mqtt.Client
{
    internal static class Program
    {
        private static async Task Main()
        {
            var transport = new TcpSocketsTransport("broker.hivemq.com", 1883);
            //var transport = new WebSocketsTransport(new Uri("ws://broker.hivemq.com:8000/mqtt"), "mqttv3.1", "mqtt");

            var reconnectPolicy = new RetryPolicyBuilder()
                //.WithTimeout(FromSeconds(15))
                .WithThreshold(100)
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithJitter(100, 1000)
                .Build();

            using(var client = new MqttClient(transport, "uzm41kyk-ibc",
                new MqttConnectionOptions {KeepAlive = 10, CleanSession = false}, true, reconnectPolicy))
            {
                client.Connected += (sender, args) =>
                    Console.WriteLine($"Connected ({(args.CleanSession ? "clean session" : "persistent session")}).");

                client.MessageReceived += (sender, m) =>
                {
                    var v = Encoding.UTF8.GetString(m.Payload.Span);
                    Console.WriteLine(m.Topic + " : " + v);
                };

                client.Disconnected += (sender, args) =>
                    Console.WriteLine(args.Aborted ? "Connection aborted." : "Disconnected.");

                await client.ConnectAsync().ConfigureAwait(false);
                await client.SubscribeAsync(new[] {("lapin/test-topic/messages", QoSLevel.ExactlyOnce)}).ConfigureAwait(true);

                await client.PublishAsync("lapin/test-topic/msg", Encoding.UTF8.GetBytes("my test packet 1"), QoSLevel.ExactlyOnce).ConfigureAwait(false);
                await client.PublishAsync("lapin/test-topic/msg", Encoding.UTF8.GetBytes("my test packet 2"), QoSLevel.AtLeastOnce).ConfigureAwait(false);
                await client.PublishAsync("lapin/test-topic/msg", Encoding.UTF8.GetBytes("my test packet 3")).ConfigureAwait(false);

                Console.WriteLine("Press any key to disconnect from MQTT server...");
                Console.ReadKey();

                await client.UnsubscribeAsync(new[] {"lapin/test-topic/messages"}).ConfigureAwait(false);
                await client.DisconnectAsync().ConfigureAwait(false);
            }
        }
    }
}