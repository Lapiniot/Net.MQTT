using System.Configuration;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mqtt.Benchmark;
using Mqtt.Benchmark.Configuration;

#pragma warning disable CA1812, CA1852 // False positive from roslyn analyzer

if (args.Length > 0 && args[0] is "--version" or "-v")
{
    Console.OutputEncoding = Encoding.UTF8;

    var assembly = Assembly.GetExecutingAssembly();
    var description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()!.Description;
    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()!.Copyright;

    Console.WriteLine($"{description} v{version} ({copyright})");

    return;
}

var builder = Host.CreateDefaultBuilder()
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureAppConfiguration((_, configuration) => configuration.AddCommandArguments(args, false))
    .ConfigureServices((_, services) => services
        .AddHostedService<BenchmarkRunnerService>()
        .AddTransient<IOptionsFactory<BenchmarkOptions>, BenchmarkOptionsFactory>());

await builder.Build().RunAsync().ConfigureAwait(false);