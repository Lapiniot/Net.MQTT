using System.Diagnostics;
using System.Net.Mqtt;
using System.Text;

namespace Mqtt.Client;

internal static class LoadTests
{
    internal static async Task PublishConcurrentTestAsync(MqttClientBuilder clientBuilder, int numClients, int numMessages, QoSLevel qosLevel,
        CancellationToken cancellationToken)
    {
        var clients = new List<MqttClient>();
        for(int i = 0; i < numClients; i++)
        {
            clients.Add(clientBuilder.BuildV4());
        }

        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var payload = Encoding.UTF8.GetBytes(id);

        Console.WriteLine($"Starting concurrent publish test.\nNumber of clients: {numClients}\nNumber of messages: {numMessages}\nQoS level: {qosLevel}");

        await ConnectAllAsync(clients, cancellationToken).ConfigureAwait(false);

        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();
            await RunAllAsync(clients, async (client, token) =>
            {
                for(int i = 0; i < numMessages; i++)
                {
                    await client.PublishAsync($"TEST/{id}/MSG{i:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("Elapsed time: {0} ({1} ms.)", stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            stopwatch.Stop();
            await DisconnectAllAsync(clients).ConfigureAwait(false);
        }
    }

    internal static async Task PublishReceiveConcurrentTestAsync(MqttClientBuilder clientBuilder, int numClients, int numMessages, QoSLevel qosLevel,
        CancellationToken cancellationToken)
    {
        using var evt = new CountdownEvent(numClients * numMessages);
        var clients = new List<MqttClient>();
        for(int i = 0; i < numClients; i++)
        {
            clients.Add(clientBuilder.BuildV4());
        }

        void OnReceived(object sender, MessageReceivedEventArgs e)
        {
            evt.Signal();
        }

        var id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
        var payload = Encoding.UTF8.GetBytes(Base32.ToBase32String(CorrelationIdGenerator.GetNext()));

        Console.WriteLine($"Starting concurrent publish/receive test.\nNumber of clients: {numClients}\nNumber of messages: {numMessages}\nQoS level: {qosLevel}");

        await ConnectAllAsync(clients, cancellationToken).ConfigureAwait(false);

        await RunAllAsync(clients, (client, token) =>
        {
            client.MessageReceived += OnReceived;
            return client.SubscribeAsync(new[] { ($"TEST/{id}/#", QoSLevel.QoS0) }, token);
        }, cancellationToken).ConfigureAwait(false);

        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();
            await RunAllAsync(clients, async (client, token) =>
            {
                for(int i = 0; i < numMessages; i++)
                {
                    await client.PublishAsync($"TEST/{id}/MSG{i:D6}", payload, qosLevel, cancellationToken: token).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);

            evt.Wait(cancellationToken);

            stopwatch.Stop();
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("Elapsed time: {0} ({1} ms.)", stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            stopwatch.Stop();
            await RunAllAsync(clients, (client, token) =>
            {
                client.MessageReceived -= OnReceived;
                return client.DisconnectAsync();
            }, cancellationToken).ConfigureAwait(false);
            await DisconnectAllAsync(clients).ConfigureAwait(false);
        }
    }

    private static async Task ConnectAllAsync(List<MqttClient> clients, CancellationToken cancellationToken)
    {
        await Task.WhenAll(clients.Select(client => client.ConnectAsync(new MqttConnectionOptions(KeepAlive: 20), cancellationToken))).ConfigureAwait(false);
    }

    private static async Task DisconnectAllAsync(List<MqttClient> clients)
    {
        await Task.WhenAll(clients.Select(client => client.DisconnectAsync())).ConfigureAwait(false);
        await Task.WhenAll(clients.Select(client => client.DisposeAsync().AsTask())).ConfigureAwait(false);
    }

    private static async Task RunAllAsync(List<MqttClient> clients, Func<MqttClient, CancellationToken, Task> func, CancellationToken cancellationToken)
    {
        await Task.WhenAll(clients.Select(client => func(client, cancellationToken))).ConfigureAwait(false);
    }
}