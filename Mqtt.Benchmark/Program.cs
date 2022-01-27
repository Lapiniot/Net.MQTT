using System.Configuration;
using Microsoft.Extensions.Configuration;
using Mqtt.Benchmark;
using Mqtt.Benchmark.Configuration;

#pragma warning disable CA1812 // False positive from roslyn analyzer

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, false)
    .AddEnvironmentVariables()
    .AddCommandArguments(args, false)
    .Build();

var options = OptionsReader.Read(configuration);

var clientBuilder = new MqttClientBuilder()
    .WithClientId(options.ClientId)
    .WithUri(options.Server)
    .WithReconnect(ShouldRepeat);

try
{
    Console.CursorVisible = false;
    Console.ForegroundColor = ConsoleColor.Gray;

    var profile = options.BuildProfile();

    switch(profile.Kind)
    {
        case "publish":
            await LoadTests.PublishTestAsync(clientBuilder, profile).ConfigureAwait(false);
            break;
        case "publish_receive":
            await LoadTests.PublishReceiveTestAsync(clientBuilder, profile).ConfigureAwait(false);
            break;
        case "subscribe_publish_receive":
            await LoadTests.SubscribePublishReceiveTestAsync(clientBuilder, profile).ConfigureAwait(false);
            break;
        default:
            throw new ArgumentException("Unknown test kind value.");
    }
}
catch(OperationCanceledException)
{
    Console.ForegroundColor = ConsoleColor.DarkRed;
    await Console.Error.WriteLineAsync($"Timeout. Overall test execution time has reached configured timeout ({options.TimeoutOverall:hh\\:mm\\:ss}).").ConfigureAwait(false);
}
catch(Exception exception)
{
    Console.ForegroundColor = ConsoleColor.DarkRed;
    await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
}
finally
{
    Console.CursorVisible = true;
    Console.ResetColor();
}

static bool ShouldRepeat(Exception ex, int attempt, TimeSpan total, ref TimeSpan delay)
{
    if(attempt <= 5)
    {
        delay = TimeSpan.FromSeconds(1 << attempt);
    }

    return attempt < 100;
}