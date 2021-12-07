using System.Diagnostics;
using System.Net.Mqtt;
using System.Text;

namespace Mqtt.Client;

internal static class LoadTests
{
    internal static async Task PublishConcurrentTestAsync(MqttClientBuilder clientBuilder, int numClients, int numMessages, QoSLevel qosLevel)
    {
        var clients = new List<MqttClient>();
        for(int i = 0; i < numClients; i++)
        {
            clients.Add(clientBuilder.BuildV4());
        }

        Console.WriteLine($"Starting concurrent publish test.\nNumber of clients: {numClients}\nNumber of messages: {numMessages}\nQoS level: {qosLevel}");
        await Task.WhenAll(clients.Select(client => client.ConnectAsync(new MqttConnectionOptions(KeepAlive: 20)))).ConfigureAwait(false);
        using(new MeasureTimeScope())
        {
            await Task.WhenAll(clients.Select(async client =>
            {
                for(int i = 0; i < numMessages; i++)
                {
                    var topic = $"lapin/test-topic/msg{i:D6}";
                    var payload = Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));
                    await client.PublishAsync(topic, payload, qosLevel).ConfigureAwait(false);
                }
            })).ConfigureAwait(false);
        }
        await Task.WhenAll(clients.Select(client => client.DisconnectAsync())).ConfigureAwait(false);
        await Task.WhenAll(clients.Select(client => client.DisposeAsync().AsTask())).ConfigureAwait(false);
    }
}