using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mqtt.Benchmark;
using Mqtt.Benchmark.Configuration;

#pragma warning disable CA1812 // False positive from roslyn analyzer

var builder = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((_, configuration) => configuration.AddCommandArguments(args))
    .ConfigureServices((_, services) => services
        .AddHostedService<BenchmarkRunnerService>()
        .AddTransient<IOptionsFactory<BenchmarkOptions>, BenchmarkOptionsFactory>());

await builder.Build().RunAsync().ConfigureAwait(false);