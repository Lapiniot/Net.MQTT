using System.Configuration;
using System.Reflection;
using System.Text;

namespace Mqtt.Benchmark;

internal static partial class Program
{
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL3050:RequiresDynamicCode")]
    private static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "--version" or "-v")
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine();
            Console.WriteLine(Assembly.GetExecutingAssembly().BuildLogoString());
            Console.WriteLine();
            return;
        }

        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });

        builder.Configuration.AddCommandArguments(args, false);
        builder.Services
            .AddHostedService<BenchmarkRunnerService>()
            .AddTransient<IOptionsFactory<BenchmarkOptions>, BenchmarkOptionsFactory>()
            .AddHttpClient("WS-CONNECT")
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler() { EnableMultipleHttp2Connections = true })
                .Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

        await builder.Build().RunAsync().ConfigureAwait(false);
    }
}