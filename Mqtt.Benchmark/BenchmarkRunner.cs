using Net.Mqtt.Client;

namespace Mqtt.Benchmark;

#pragma warning disable CA1812

internal sealed class BenchmarkRunner(IHttpMessageHandlerFactory handlerFactory,
    IOptions<BenchmarkOptions> benchmarkOptions,
    IHostApplicationLifetime hostApplicationLifetime)
{
    public async Task RunAsync(CancellationToken stoppingToken = default)
    {
        using var jointCts = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken,
            hostApplicationLifetime.ApplicationStopping);
        var token = jointCts.Token;

        try
        {
            var options = benchmarkOptions.Value;

            using var invoker = new HttpMessageInvoker(handlerFactory.CreateHandler("WS-CONNECT"), false);
            var clientBuilder = new MqttClientBuilder()
                .WithWebSocketOptions(o =>
                {
                    o.HttpVersion = HttpVersion.Version20;
                    o.HttpVersionPolicy = options.ForceHttp2 ? HttpVersionPolicy.RequestVersionExact : default;
                })
                .WithWebSocketHttpMessageInvoker(invoker)
                .WithUri(options.Server);

            if (options.Protocol is Protocol.Auto)
            {
                for (var version = 5; version >= 3; version--)
                {
                    clientBuilder = clientBuilder.WithProtocol(version);
                    try
                    {
                        var client = clientBuilder.Build();
                        await using (client.ConfigureAwait(false))
                        {
                            await client.ConnectAsync(token).ConfigureAwait(false);
                            await client.DisconnectAsync().ConfigureAwait(false);
                            break;
                        }
                    }
#pragma warning disable CA1031
                    catch
#pragma warning restore CA1031
                    {
                        // expected
                    }
                }
            }
            else
            {
                clientBuilder = clientBuilder.WithProtocol((int)options.Protocol);
            }

            try
            {
                Console.CursorVisible = false;
                Console.ForegroundColor = ConsoleColor.Gray;

                switch (options.Kind)
                {
                    case "publish":
                        await LoadTests.PublishTestAsync(options.Server, clientBuilder, options, token).ConfigureAwait(false);
                        break;
                    case "publish_receive":
                        await LoadTests.PublishReceiveTestAsync(options.Server, clientBuilder, options, token).ConfigureAwait(false);
                        break;
                    case "subscribe_publish_receive":
                        await LoadTests.SubscribePublishReceiveTestAsync(options.Server, clientBuilder, options, token).ConfigureAwait(false);
                        break;
                    default:
                        ThrowUnknownTestKind();
                        break;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                await Console.Error.WriteLineAsync("\n\nTest haven't finished. Aborted by user.\n").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                await Console.Error.WriteLineAsync($"\n\nTest haven't finished. Overall test execution time has reached configured timeout ({options.TimeoutOverall:hh\\:mm\\:ss}).\n").ConfigureAwait(false);
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
        }
    }

    [DoesNotReturn]
    private static void ThrowUnknownTestKind() =>
        throw new ArgumentException("Unknown test kind value.");
}