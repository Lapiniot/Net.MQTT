using System.Configuration;
using System.Net.Mqtt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Mqtt.Client.Configuration;
using static System.Text.Encoding;

#pragma warning disable CA1812 // False positive from roslyn analyzer

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, false)
    .AddEnvironmentVariables()
    .AddCommandArguments(args)
    .Build();

var options = configuration.Get<ClientOptions>();

var client = new MqttClientBuilder()
    .WithClientId(options.ClientId)
    .WithUri(options.Server)
    .WithReconnect(ShouldRepeat)
    .BuildV4();

await using(client.ConfigureAwait(false))
{
    Console.Clear();

    client.Connected += (sender, args) => Console.WriteLine($"Connected ({(args.CleanSession ? "clean session" : "persistent session")}).");

    client.MessageReceived += (sender, m) => Console.WriteLine(m.Topic + " : " + Encoding.UTF8.GetString(m.Payload.Span));

    client.Disconnected += (sender, args) => Console.WriteLine(args.Aborted ? "Connection aborted." : "Disconnected.");
    await client.ConnectAsync(new MqttConnectionOptions(false, 120), waitForAcknowledgement: false).ConfigureAwait(false);
    //await client.SubscribeAsync(new[] { ("lapin/test-topic/messages", QoSLevel.QoS1) }).ConfigureAwait(true);

    for(int i = 0; i < 100000; i++)
    {
        await client.PublishAsync($"lapin/test-topic/msg{i:D6}",
            UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext())),
            qosLevel: QoSLevel.QoS1).ConfigureAwait(false);
    }

    Console.WriteLine("Press any key to disconnect from MQTT server...");
    Console.ReadKey();

    //await client.UnsubscribeAsync(new[] { "lapin/test-topic/messages" }).ConfigureAwait(false);
    await client.DisconnectAsync().ConfigureAwait(false);
}

static bool ShouldRepeat(Exception ex, int attempt, TimeSpan total, ref TimeSpan delay)
{
    delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
    return attempt < 100;
}