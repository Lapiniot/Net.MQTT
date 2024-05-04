namespace Mqtt.Benchmark;

public class BenchmarkRunnerService(IHostApplicationLifetime applicationLifetime,
    IHttpMessageHandlerFactory handlerFactory, IOptions<BenchmarkOptions> options) :
    BackgroundService
{
    private readonly IOptions<BenchmarkOptions> options = options;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var options = this.options.Value;
            var profile = options.BuildProfile();

            using var invoker = new HttpMessageInvoker(handlerFactory.CreateHandler("WS-CONNECT"), false);
            var clientBuilder = new MqttClientBuilder()
                .WithWebSocketOptions(o =>
                {
                    o.HttpVersion = HttpVersion.Version20;
                    o.HttpVersionPolicy = options.ForceHttp2 ? HttpVersionPolicy.RequestVersionExact : default;
                })
                .WithWebSocketHttpMessageInvoker(invoker)
                .WithUri(options.Server);

            try
            {
                Console.CursorVisible = false;
                Console.ForegroundColor = ConsoleColor.Gray;

                switch (profile.Kind)
                {
                    case "publish":
                        await LoadTests.PublishTestAsync(options.Server, clientBuilder, profile, stoppingToken).ConfigureAwait(false);
                        break;
                    case "publish_receive":
                        await LoadTests.PublishReceiveTestAsync(options.Server, clientBuilder, profile, stoppingToken).ConfigureAwait(false);
                        break;
                    case "subscribe_publish_receive":
                        await LoadTests.SubscribePublishReceiveTestAsync(options.Server, clientBuilder, profile, stoppingToken).ConfigureAwait(false);
                        break;
                    default:
                        ThrowUnknownTestKind();
                        break;
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
                await Console.Error.WriteLineAsync($"\n\nTest haven't finished. Overall test execution time has reached configured timeout ({profile.TimeoutOverall:hh\\:mm\\:ss}).\n").ConfigureAwait(false);
            }
        }
#pragma warning disable CA1031
        catch (Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
        }
#pragma warning restore CA1031
        finally
        {
            Console.CursorVisible = true;
            Console.ResetColor();
            applicationLifetime.StopApplication();
        }
    }

    [DoesNotReturn]
    private static void ThrowUnknownTestKind() =>
        throw new ArgumentException("Unknown test kind value.");
}