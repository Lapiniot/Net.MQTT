using Mqtt.Benchmark;
using OOs.CommandLine.Generated;
using OOs.Extensions.Configuration;
using System.Reflection;
using System.Text;

if (args.Length > 0 && args[0] is "--version" or "-v")
{
    Console.OutputEncoding = Encoding.UTF8;
    Console.WriteLine();
    Console.WriteLine(Assembly.GetExecutingAssembly().BuildLogoString());
    Console.WriteLine();
    return;
}

var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });

builder.Configuration.AddCommandArguments<ArgumentParser>(args);

builder.Services
    .AddTransient<BenchmarkRunner>()
    .AddTransient<IConfigureOptions<BenchmarkOptions>, BenchmarkOptionsSetup>()
    .AddHttpClient("WS-CONNECT")
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler() { EnableMultipleHttp2Connections = true })
        .Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

var host = builder.Build();
await host.StartAsync().ConfigureAwait(false);

var runner = host.Services.GetRequiredService<BenchmarkRunner>();
var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await runner.RunAsync(applicationLifetime.ApplicationStopping).ConfigureAwait(false);