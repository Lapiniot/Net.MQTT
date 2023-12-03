using CommandLine;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config: BuildGlobalConfig(args));

internal sealed partial class Program
{
    private static readonly char[] Separators = [',', ';', ' '];

    private static ManualConfig BuildGlobalConfig(string[] args)
    {
        var config = ManualConfig.CreateMinimumViable()
            .WithOption(ConfigOptions.DisableLogFile, true)
            .WithOption(ConfigOptions.LogBuildOutput, false)
            .WithOption(ConfigOptions.GenerateMSBuildBinLog, false)
            .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage));

        using var parser = new Parser(s => s.IgnoreUnknownArguments = true);

        if (parser.ParseArguments<CommandLineOptions>(args) is Parsed<CommandLineOptions> { Value.Filters: { } filters })
        {
            var sampleSets = new List<string>();
            foreach (var item in filters)
            {
                if (item.StartsWith("samples:", StringComparison.OrdinalIgnoreCase))
                {
                    sampleSets.AddRange(item.Substring(8).Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
            }

            if (sampleSets.Count > 0)
            {
                config.AddFilter(new SampleSetsFilter([.. sampleSets]));
            }
        }

        config.AddJob(Job.Default
            .WithArguments([new MsBuildArgument("/p:UseArtifactsOutput=false")])
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitDisasm", ""))
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitDiffableDasm", "1"))
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitStdOutFile", ""))
        );

        return config;
    }
}