using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using v1 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV1;
using v2 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV2;
using v3 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV3;
using v4 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV4;
using v5 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV5;
using v6 = System.Net.Mqtt.Extensions.MqttExtensions;

#pragma warning disable CA1822, CA1812

namespace System.Net.Mqtt.Benchmarks.Extensions;

[Config(typeof(Config))]
[SampleSetsFilter("large", "medium", "small")]
[HideColumns("Error", "StdDev", "RatioSD", "Median")]
public class TopicMatchingBenchmarks
{
    public static IEnumerable<SampleSet> Samples { get; } = new SampleSet[]
    {
        new("Small", new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
        {
            ("l1/l2/l3"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/"u8.ToArray()),
            ("l1/l2/"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("0l1/l2/l3"u8.ToArray(), "1l1/l2/l3"u8.ToArray()),
            ("l1/l2/0l3"u8.ToArray(), "l1/l2/1l3"u8.ToArray()),
            ("l1/l2/l3/l4/l5/l6"u8.ToArray(), "l1/l2/l3/l4/l5/l6"u8.ToArray()),
            ("0l1/l2/l3/l4/l5/l6"u8.ToArray(), "1l1/l2/l3/l4/l5/l6"u8.ToArray()),
            ("l1/l2/l3/l4/l5/0l6"u8.ToArray(), "l1/l2/l3/l4/l5/1l6"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/l3/#"u8.ToArray()),
            ("l1/l2/l3/"u8.ToArray(), "l1/l2/l3/#"u8.ToArray()),
            ("l1/l2/l3/l4"u8.ToArray(), "l1/l2/l3/#"u8.ToArray()),
            ("l1/l2/l333"u8.ToArray(), "l1/l2/l3/#"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/+"u8.ToArray()),
            ("l1/l2/"u8.ToArray(), "l1/l2/+"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/l2/+"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/+/l3"u8.ToArray()),
            ("l1//l3"u8.ToArray(), "l1/+/l3"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/+/l3"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l1/0l2/l3"u8.ToArray(), "+/1l2/l3"u8.ToArray()),
            ("/l2/l3"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l1/l3"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l1/l0/l2/l3"u8.ToArray(), "l1/+/l2/#"u8.ToArray()),
            ("test/0l1/l2/l3"u8.ToArray(), "+/1l1/l2/#"u8.ToArray()),
            ("test/l1/l2/l3"u8.ToArray(), "+/l1/l2/#"u8.ToArray()),
            ("test/l1/l2"u8.ToArray(), "+/l1/+/#"u8.ToArray()),
            ("test/l1/l2/"u8.ToArray(), "+/l1/+/#"u8.ToArray()),
            ("test/l1/l2/l3"u8.ToArray(), "+/+/+/+"u8.ToArray()),
            ("test/l1/l2/l3"u8.ToArray(), "+/+/+"u8.ToArray()),
            ("test/l1/l2/l3"u8.ToArray(), "+/+/+/#"u8.ToArray()),
            ("test/l1/l2/l3"u8.ToArray(), "+/+/#"u8.ToArray()),
            ("test/l1/l2/l3"u8.ToArray(), "+/#"u8.ToArray()),
            ("test/l1/l2/l3"u8.ToArray(), "#"u8.ToArray())
        }),
        new("Medium", new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
        {
            ("level1/level2/level3"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/"u8.ToArray()),
            ("level1/level2/"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("0level1/level2/level3"u8.ToArray(), "1level1/level2/level3"u8.ToArray()),
            ("level1/level2/0level3"u8.ToArray(), "level1/level2/1level3"u8.ToArray()),
            ("level1/level2/level3/level4/level5/level6"u8.ToArray(), "level1/level2/level3/level4/level5/level6"u8.ToArray()),
            ("0level1/level2/level3/level4/level5/level6"u8.ToArray(), "1level1/level2/level3/level4/level5/level6"u8.ToArray()),
            ("level1/level2/level3/level4/level5/0level6"u8.ToArray(), "level1/level2/level3/level4/level5/1level6"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/level3/#"u8.ToArray()),
            ("level1/level2/level3/"u8.ToArray(), "level1/level2/level3/#"u8.ToArray()),
            ("level1/level2/level3/level4"u8.ToArray(), "level1/level2/level3/#"u8.ToArray()),
            ("level1/level2/level333"u8.ToArray(), "level1/level2/level3/#"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/+"u8.ToArray()),
            ("level1/level2/"u8.ToArray(), "level1/level2/+"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/level2/+"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/+/level3"u8.ToArray()),
            ("level1//level3"u8.ToArray(), "level1/+/level3"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/+/level3"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level1/0level2/level3"u8.ToArray(), "+/1level2/level3"u8.ToArray()),
            ("/level2/level3"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level1/level3"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level1/level0/level2/level3"u8.ToArray(), "level1/+/level2/#"u8.ToArray()),
            ("test/0level111/level222/level333"u8.ToArray(), "+/1level111/level2/#"u8.ToArray()),
            ("test/level111/level222/level333"u8.ToArray(), "+/level111/level2/#"u8.ToArray()),
            ("test/level111/level222"u8.ToArray(), "+/level111/+/#"u8.ToArray()),
            ("test/level111/level222/"u8.ToArray(), "+/level111/+/#"u8.ToArray()),
            ("test/level111/level222/level333"u8.ToArray(), "+/+/+/+"u8.ToArray()),
            ("test/level111/level222/level333"u8.ToArray(), "+/+/+"u8.ToArray()),
            ("test/level111/level222/level333"u8.ToArray(), "+/+/+/#"u8.ToArray()),
            ("test/level111/level222/level333"u8.ToArray(), "+/+/#"u8.ToArray()),
            ("test/level111/level222/level333"u8.ToArray(), "+/#"u8.ToArray()),
            ("test/level111/level222/level333"u8.ToArray(), "#"u8.ToArray())
        }),
        new("Large", new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
        {
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("0testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "1testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/0testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/1testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/testtopiclevel6"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/testtopiclevel6"u8.ToArray()),
            ("0testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/testtopiclevel6"u8.ToArray(), "1testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/testtopiclevel6"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/0testtopiclevel6"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/1testtopiclevel6"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3/"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel333"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/+"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/"u8.ToArray(), "testtopiclevel1/testtopiclevel2/+"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "testtopiclevel1/testtopiclevel2/+"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/+/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1//testtopiclevel3"u8.ToArray(), "testtopiclevel1/+/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "testtopiclevel1/+/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/0testtopiclevel2/testtopiclevel3"u8.ToArray(), "+/1testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("/testtopiclevel2/testtopiclevel3"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel3"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel0/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/+/testtopiclevel2/#"u8.ToArray()),
            ("test/0testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/1testtopiclevel111/testtopiclevel2/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/testtopiclevel111/testtopiclevel2/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222"u8.ToArray(), "+/testtopiclevel111/+/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/"u8.ToArray(), "+/testtopiclevel111/+/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/+/+/+"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/+/+"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/+/+/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/+/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "#"u8.ToArray())
        })
    };

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV1([NotNull] SampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            v1.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV2([NotNull] SampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            v2.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV3([NotNull] SampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            v3.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV4([NotNull] SampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            v4.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV5([NotNull] SampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            v5.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV6([NotNull] SampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            v6.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    private sealed class Config : ManualConfig
    {
        public Config()
        {
            SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage);
            Options &= ~(ConfigOptions.LogBuildOutput | ConfigOptions.GenerateMSBuildBinLog);
            Options |= ConfigOptions.DisableLogFile;
        }
    }
}

public sealed class SampleSet : SampleSet<ValueTuple<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>
{
    private string? displayString;

    public SampleSet(string name, (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[] samples) : base(name, samples) { }

    public override string ToString() => displayString ??= GetDisplayString();

    private string GetDisplayString()
    {
        var span = Samples.AsSpan();

        if (span.Length == 0) return Name;

        int t_sum = 0, f_sum = 0;

        for (var i = 0; i < span.Length; i++)
        {
            t_sum += span[i].Item1.Length;
            f_sum += span[i].Item2.Length;
        }

        return $"{Name} ({Math.Round((double)t_sum / span.Length)}/{Math.Round((double)f_sum / span.Length)})";
    }
}