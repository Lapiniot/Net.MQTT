using System.Configuration;
using Microsoft.Extensions.Configuration;
using Mqtt.Client;
using Mqtt.Client.Configuration;

#pragma warning disable CA1812 // False positive from roslyn analyzer

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, false)
    .AddEnvironmentVariables()
    .AddCommandArguments(args, false)
    .Build();

var options = configuration.Get<ClientOptions>();

var clientBuilder = new MqttClientBuilder()
    .WithClientId(options.ClientId)
    .WithUri(options.Server)
    .WithReconnect(ShouldRepeat);

Console.Clear();

using var cts = new CancellationTokenSource(options.TimeoutOverall);
switch(options.TestName)
{
    case "publish" or "test0":
        await LoadTests.PublishConcurrentTestAsync(clientBuilder, options.NumClients, options.NumMessages, options.QoSLevel, cts.Token).ConfigureAwait(false);
        break;
    case "publish_receive" or "test1":
        await LoadTests.PublishReceiveConcurrentTestAsync(clientBuilder, options.NumClients, options.NumMessages, options.QoSLevel, cts.Token).ConfigureAwait(false);
        break;
    default:
        await Console.Error.WriteLineAsync("Unknown test name value").ConfigureAwait(false);
        break;
}

static bool ShouldRepeat(Exception ex, int attempt, TimeSpan total, ref TimeSpan delay)
{
    delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
    return attempt < 100;
}