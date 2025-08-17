using Mqtt.Benchmark;
using OOs.Extensions.Configuration;

[assembly: GenerateProductInfo]

var (options, arguments) = Arguments.Parse(args);

if (options.TryGetValue("PrintVersion", out var value) && bool.TryParse(value, out var printVersion) && printVersion)
{
    Console.WriteLine();
    Console.WriteLine($"{ProductInfo.Description} v{ProductInfo.InformationalVersion} ({ProductInfo.Copyright})");
    Console.WriteLine();
    return;
}
else if (options.TryGetValue("PrintHelp", out value) && bool.TryParse(value, out var printHelp) && printHelp)
{
    Console.WriteLine();
    Console.WriteLine($"Usage: {Path.GetFileName(Environment.ProcessPath)} [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine(Arguments.GetSynopsis());
    Console.WriteLine();
    return;
}

var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });

builder.Configuration.AddCommandArguments(options, arguments);

builder.Services
    .AddTransient<BenchmarkRunner>()
    .AddTransient<IConfigureOptions<BenchmarkOptions>, BenchmarkOptionsSetup>()
    .AddHttpClient("WS-CONNECT")
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler() { EnableMultipleHttp2Connections = true })
        .Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

var host = builder.Build();
await host.StartAsync().ConfigureAwait(false);

var runner = host.Services.GetRequiredService<BenchmarkRunner>();
await runner.RunAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

await host.StopAsync().ConfigureAwait(false);