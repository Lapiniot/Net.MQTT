﻿using static System.Text.Encoding;

#pragma warning disable CA1812 // False positive from roslyn analyzer

//using var certificate = new X509Certificate2("./mqtt-client.pfx");
#pragma warning disable CA2000 // False positive from roslyn analyzer
var transport = NetworkTransportFactory.CreateWebSockets(new Uri("https://mqtt-server:8002/mqtt"));
#pragma warning restore CA2000
await using(transport.ConfigureAwait(false))
{
#pragma warning disable CA2000 // False positive from roslyn analyzer
    var client = new MqttClientBuilder().WithTransport(transport).WithClientId("uzm41kyk-ibc").WithReconnect(ShouldRepeat).BuildV4();
#pragma warning restore CA2000
    await using(client.ConfigureAwait(false))
    {

        //client.Connected += (sender, args) => Console.WriteLine($"Connected ({(args.CleanSession ? "clean session" : "persistent session")}).");

        //client.MessageReceived += (sender, m) => Console.WriteLine(m.Topic + " : " + Encoding.UTF8.GetString(m.Payload.Span));

        //client.Disconnected += (sender, args) => Console.WriteLine(args.Aborted ? "Connection aborted." : "Disconnected.");

        await client.ConnectAsync(new MqttConnectionOptions(false, 120), waitForAcknowledgement: false).ConfigureAwait(false);
        //await client.SubscribeAsync(new[] { ("lapin/test-topic/messages", QoSLevel.QoS1) }).ConfigureAwait(true);

        //client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1"));
        //client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2"));
        //client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3"));
        //client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 4"));
        //client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2"), QoSLevel.QoS1);
        //client.Publish("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3"), QoSLevel.QoS2);

        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 1 (async test)")).ConfigureAwait(false);
        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2 (async test)")).ConfigureAwait(false);
        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3 (async test)")).ConfigureAwait(false);
        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 4 (async test)")).ConfigureAwait(false);
        await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 5 (async test)")).ConfigureAwait(false);
        //await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 2 (async test)"), QoSLevel.QoS1).ConfigureAwait(false);
        //await client.PublishAsync("lapin/test-topic/msg", UTF8.GetBytes("my test packet 3 (async test)"), QoSLevel.QoS2).ConfigureAwait(false);

        //Console.WriteLine("Press any key to disconnect from MQTT server...");
        //Console.ReadKey();

        //await client.UnsubscribeAsync(new[] { "lapin/test-topic/messages" }).ConfigureAwait(false);
        await client.DisconnectAsync().ConfigureAwait(false);
    }
}

static bool ShouldRepeat(Exception ex, int attempt, TimeSpan total, ref TimeSpan delay)
{
    delay = TimeSpan.FromSeconds(5);
    return attempt < 100;
}