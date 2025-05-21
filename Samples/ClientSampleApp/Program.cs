using System.Text;
using Net.Mqtt;
using Net.Mqtt.Client;
using OOs;

var path = OperatingSystem.IsWindows()
    ? "%TEMP%/mqttd.sock"
    : "/tmp/mqttd.sock";

// Build MQTT client instance:
var client = new MqttClientBuilder()
    .WithClientId($"nmqtt-client-{Base32.ToBase32String(CorrelationIdGenerator.GetNext())}")
// Use locally deployed MQTT server with Unix Domain Sockets endpoint 
    .WithUnixDomain(new(path))
// Use locally deployed MQTT server with TCP sockets endpoint 
//  .WithTcp("[::]", 1883)
// Use locally deployed MQTT server with WebSockets endpoint 
//  .WithWebSockets(new Uri("http://localhost:8001/mqtt"))
// Build MqttClient5 instance specifically
    .BuildV5();

// Subscribe to the client's lifecycle events
client.Connected += OnConnected;
client.Disconnected += OnDisconnected;
client.Message5Received += OnReceived;

// Attach custom external observer in order to listen for received messages
using var observation = client.SubscribeMessageObserver(new MessageObserver());

await using (client.ConfigureAwait(false))
{
    // Connect to the MQTT server with non-default MqttConnectionOptions5
    await client.ConnectAsync(new(CleanStart: true)
    {
        MaxPacketSize = 640,
        SessionExpiryInterval = 600
    }).ConfigureAwait(false);

    // Subscribe to the "testtopic/#" with non-default subscribe options
    await client.SubscribeAsync([("testtopic/#", new(QoSLevel.QoS2, false, false, RetainHandling.SendAlways))]).ConfigureAwait(false);

    Console.WriteLine("Press any key to exit...");

    var message = new Message("testtopic/topic1"u8.ToArray(), "hello from MQTT v5 client!"u8.ToArray(), QoSLevel.QoS2, Retain: true)
    {
        ContentType = "text/plain"u8.ToArray(),
        PayloadFormat = true,
        CorrelationData = "correlation data"u8.ToArray(),
        ResponseTopic = "responses/topic1"u8.ToArray(),
        ExpiryInterval = 120,
        UserProperties = [("prop1"u8.ToArray(), "v1"u8.ToArray()), ("prop2"u8.ToArray(), "v2"u8.ToArray())]
    };

    // Publish retained test message to the "testtopic/topic1" with QoS2
    await client.PublishAsync(message, default).ConfigureAwait(false);
    // Publish non-retianed test message to the "testtopic/topic2" with QoS1
    await client.PublishAsync(message with { Topic = "testtopic/topic2"u8.ToArray(), QoSLevel = QoSLevel.QoS1, Retain = false }, default).ConfigureAwait(false);
    // Publish non-retianed test message to the "testtopic/topic3" with QoS0
    await client.PublishAsync(message with { Topic = "testtopic/topic3"u8.ToArray(), QoSLevel = QoSLevel.QoS0, Retain = false }, default).ConfigureAwait(false);

    Console.ReadKey();
    // Unsubscribe from "testtopic/#" topic
    await client.UnsubscribeAsync(["testtopic/#"]).ConfigureAwait(false);
    // Disconnect from the server gracefully
    await client.DisconnectAsync().ConfigureAwait(false);
}

client.Message5Received -= OnReceived;
client.Disconnected -= OnDisconnected;
client.Connected -= OnConnected;

static void OnDisconnected(object? sender, DisconnectedEventArgs e) =>
    Console.WriteLine($"Disconnected (graceful: {e.Graceful})");

static void OnConnected(object? _, ConnectedEventArgs e) =>
    Console.WriteLine($"Connected (clean start: {e.Clean})");

static void OnReceived(object? _, MqttMessageArgs<MqttMessage5> args)
{
    Console.WriteLine($"""
Incoming message:
Topic:      {Encoding.UTF8.GetString(args.Message.Topic.Span)}
Retained:   {args.Message.Retained}
Payload:    {Encoding.UTF8.GetString(args.Message.Payload.Span)}
""");
}

internal sealed class MessageObserver : IObserver<MqttMessage5>
{
    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(MqttMessage5 value)
    {
        // Invoked for every message received
    }
}