using System;
using System.Net.Mqtt.Client;
using System.Net.Transports;
using System.Policies;
using System.Threading.Tasks;
using static System.Net.Mqtt.QoSLevel;
using static System.Text.Encoding;

namespace Mqtt.Client
{
    internal static class Program
    {
        private static async Task Main()
        {
            //var transport = new TcpSocketsTransport("mqtt-server", 1883);
            //var transport = new TcpSocketsTransport("broker.hivemq.com", 1883);
            var transport = new WebSocketsTransport(new Uri("ws://broker.hivemq.com:8000/mqtt"), "mqttv3.1", "mqtt");
            //var transport = new WebSocketsTransport(new Uri("ws://localhost:8000/mqtt"), "mqttv3.1", "mqtt");

            var reconnectPolicy = new RetryPolicyBuilder()
                //.WithTimeout(FromSeconds(15))
                .WithThreshold(100)
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithJitter(100, 1000)
                .Build();

            using(var client = new MqttClient(transport, "uzm41kyk-ibc",
                new MqttConnectionOptions {KeepAlive = 10, CleanSession = false}, reconnectPolicy))
            {
                client.Connected += (sender, args) =>
                    Console.WriteLine($"Connected ({(args.CleanSession ? "clean session" : "persistent session")}).");

                client.MessageReceived += (sender, m) =>
                {
                    var v = UTF8.GetString(m.Payload.Span);
                    Console.WriteLine(m.Topic + " : " + v);
                };

                client.Disconnected += (sender, args) =>
                    Console.WriteLine(args.Aborted ? "Connection aborted." : "Disconnected.");

                await client.ConnectAsync().ConfigureAwait(false);
                await client.SubscribeAsync(new[] {("lapin/test-topic/messages", QoS1)}).ConfigureAwait(true);

                client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1"));
                client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2"), QoS1);
                client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3"), QoS2);

                await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1 (async test)")).ConfigureAwait(false);
                await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2 (async test)"), QoS1).ConfigureAwait(false);
                await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3 (async test)"), QoS2).ConfigureAwait(false);

                Console.WriteLine("Press any key to disconnect from MQTT server...");
                Console.ReadKey();

                await client.UnsubscribeAsync(new[] {"lapin/test-topic/messages"}).ConfigureAwait(false);
                await client.DisconnectAsync().ConfigureAwait(false);
            }
        }
    }
}