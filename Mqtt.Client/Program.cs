using System.Policies;
using System.Security.Cryptography.X509Certificates;

using static System.Text.Encoding;

//Console.WriteLine("Press any key to connect...");
//Console.ReadKey();
//var connection = new TcpSocketClientConnection("broker.hivemq.com", 1883);
//var connection = new WebSocketClientConnection(new Uri("ws://broker.hivemq.com:8000/mqtt"), "mqttv3.1", "mqtt");
using var certificate = new X509Certificate2("./mqtt-client.pfx");
#pragma warning disable CA2000 // Dispose objects before losing scope - seems to be another false noise from analyzer 
var transport = NetworkTransportFactory.CreateTcpSsl("mqtt-server", 1884, certificate: certificate);
await using(transport.ConfigureAwait(false))
{
    var reconnectPolicy = new ConditionalRetryPolicy(new RepeatCondition[] { ShouldRepeat });
    var client = new MqttClient4(transport, "uzm41kyk-ibc", null, reconnectPolicy);
#pragma warning restore CA2000
    await using(client.ConfigureAwait(false))
    {

        client.Connected += (sender, args) => Console.WriteLine($"Connected ({(args.CleanSession ? "clean session" : "persistent session")}).");

        client.MessageReceived += (sender, m) =>
        {
            var v = UTF8.GetString(m.Payload.Span);
            Console.WriteLine(m.Topic + " : " + v);
        };

        client.Disconnected += (sender, args) =>
            Console.WriteLine(args.Aborted ? "Connection aborted." : "Disconnected.");

        await client.ConnectAsync(new MqttConnectionOptions(false, 120)).ConfigureAwait(false);
        await client.SubscribeAsync(new[] { ("lapin/test-topic/messages", QoSLevel.QoS1) }).ConfigureAwait(true);

        client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1"));
        client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2"), QoSLevel.QoS1);
        client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3"), QoSLevel.QoS2);

        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1 (async test)")).ConfigureAwait(false);
        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2 (async test)"), QoSLevel.QoS1).ConfigureAwait(false);
        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3 (async test)"), QoSLevel.QoS2).ConfigureAwait(false);

        Console.WriteLine("Press any key to disconnect from MQTT server...");
        Console.ReadKey();

        await client.UnsubscribeAsync(new[] { "lapin/test-topic/messages" }).ConfigureAwait(false);
        await client.DisconnectAsync().ConfigureAwait(false);
    }
}

bool ShouldRepeat(Exception ex, int attempt, TimeSpan total, ref TimeSpan delay)
{
    delay = TimeSpan.FromSeconds(5);
    return attempt < 100;
}