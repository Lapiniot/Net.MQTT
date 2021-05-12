using System;
using System.Net.Connections;
using System.Net.Mqtt;
using System.Net.Mqtt.Client;
using System.Policies;
using static System.Net.Mqtt.QoSLevel;
using static System.Text.Encoding;

//Console.WriteLine("Press any key to connect...");
//Console.ReadKey();
//var transport = new TcpSocketClientConnection("mqtt-server", 1883);
//var connection = new TcpSocketClientConnection("broker.hivemq.com", 1883);
//var connection = new WebSocketClientConnection(new Uri("ws://broker.hivemq.com:8000/mqtt"), "mqttv3.1", "mqtt");
await using var connection = new WebSocketClientConnection(new Uri("ws://mqtt-server:8888/mqtt"), new string[] { "mqttv3.1", "mqtt" });
await using var transport = new NetworkConnectionAdapterTransport(connection);

var reconnectPolicy = new ConditionalRetryPolicy(new RepeatCondition[]
{
    (Exception ex, int attempt, TimeSpan total, ref TimeSpan delay) =>
    {
        delay = TimeSpan.FromSeconds(5);
        return attempt < 100;
    }
});

await using var client = new MqttClient(transport, "uzm41kyk-ibc", null, reconnectPolicy);

client.Connected += (sender, args) => Console.WriteLine($"Connected ({(args.CleanSession ? "clean session" : "persistent session")}).");

client.MessageReceived += (sender, m) =>
{
    var v = UTF8.GetString(m.Payload.Span);
    Console.WriteLine(m.Topic + " : " + v);
};

client.Disconnected += (sender, args) =>
    Console.WriteLine(args.Aborted ? "Connection aborted." : "Disconnected.");

await client.ConnectAsync(new MqttConnectionOptions { KeepAlive = 120, CleanSession = false }).ConfigureAwait(false);
await client.SubscribeAsync(new[] { ("lapin/test-topic/messages", QoS1) }).ConfigureAwait(true);

client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1"));
client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2"), QoS1);
client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3"), QoS2);

await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1 (async test)")).ConfigureAwait(false);
await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2 (async test)"), QoS1).ConfigureAwait(false);
await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3 (async test)"), QoS2).ConfigureAwait(false);

Console.WriteLine("Press any key to disconnect from MQTT server...");
Console.ReadKey();

await client.UnsubscribeAsync(new[] { "lapin/test-topic/messages" }).ConfigureAwait(false);
await client.DisconnectAsync().ConfigureAwait(false);