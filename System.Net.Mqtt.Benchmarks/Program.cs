using System.Net.Mqtt.Benchmarks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Running;
using CommandLine;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config: BuildGlobalConfig(args));

internal sealed partial class Program
{
    private static IConfig BuildGlobalConfig(string[] args)
    {
        using var parser = new Parser(s => s.IgnoreUnknownArguments = true);

        if (parser.ParseArguments<CommandLineOptions>(args) is Parsed<CommandLineOptions> { Value.Filters: { } filters })
        {
            var sampleSets = new List<string>();
            foreach (var item in filters)
            {
                if (item.StartsWith("samples:", StringComparison.OrdinalIgnoreCase))
                {
                    sampleSets.AddRange(item.Substring(8).Split(new[] { ',', ';', ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
            }

            if (sampleSets.Count > 0)
            {
                return ManualConfig.CreateMinimumViable().AddFilter(new SampleSetsFilter(sampleSets.ToArray()));
            }
        }

        return DefaultConfig.Instance;
    }
}