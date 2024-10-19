BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config: DefaultConfig.Instance
    .WithOption(ConfigOptions.StopOnFirstError, true)
    .WithOption(ConfigOptions.DisableLogFile, true)
    .WithOption(ConfigOptions.LogBuildOutput, false)
    .WithOption(ConfigOptions.GenerateMSBuildBinLog, false)
    .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage))
    .AddJob(Job.Default
        .WithArguments([new MsBuildArgument("/p:UseArtifactsOutput=false")])
        .AsDefault()));