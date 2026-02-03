using Microsoft.Extensions.Logging;
using Net.Mqtt.Server;

using var loggerFactory = LoggerFactory.Create(builder => builder
    .SetMinimumLevel(LogLevel.Information)
    .AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.UseUtcTimestamp = true;
    }));
var logger = loggerFactory.CreateLogger<MqttServer>();

var server = new MqttServerBuilder()
    .WithTcpEndPoint()
    .WithWebSocketsEndPoint()
    .WithLogger(logger)
    .WithMQTT31()
    .WithMQTT311()
    .WithMQTT5()
    .Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKeyPressed;
Console.WriteLine("Press Ctrl+C to exit...");

await using (server.ConfigureAwait(false))
{
    await server.RunAsync(cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
}

Console.CancelKeyPress -= OnCancelKeyPressed;

void OnCancelKeyPressed(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    cts.Cancel();
}