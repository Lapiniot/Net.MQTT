using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mqtt.Benchmark.Configuration;

namespace Mqtt.Benchmark;

public class BenchmarkRunnerService : BackgroundService
{
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly IOptions<BenchmarkOptions> benchmarkOptions;

    public BenchmarkRunnerService(IHostApplicationLifetime applicationLifetime, IOptions<BenchmarkOptions> options)
    {
        this.applicationLifetime = applicationLifetime;
        benchmarkOptions = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = benchmarkOptions.Value;
        var clientBuilder = new MqttClientBuilder()
            .WithClientId(options.ClientId)
            .WithUri(options.Server);

        try
        {
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Gray;

            var profile = options.BuildProfile();

            switch (profile.Kind)
            {
                case "publish":
                    await LoadTests.PublishTestAsync(clientBuilder, profile, stoppingToken).ConfigureAwait(false);
                    break;
                case "publish_receive":
                    await LoadTests.PublishReceiveTestAsync(clientBuilder, profile, stoppingToken).ConfigureAwait(false);
                    break;
                case "subscribe_publish_receive":
                    await LoadTests.SubscribePublishReceiveTestAsync(clientBuilder, profile, stoppingToken).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentException("Unknown test kind value.");
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            await Console.Error.WriteLineAsync("\n\nTest haven't finished. Aborted by user.\n").ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            await Console.Error.WriteLineAsync($"\n\nTest haven't finished. Overall test execution time has reached configured timeout ({options.TimeoutOverall:hh\\:mm\\:ss}).\n").ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
        }
        finally
        {
            Console.CursorVisible = true;
            Console.ResetColor();
            applicationLifetime.StopApplication();
        }
    }
}