using V1 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV1;
using V2 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV2;
using V3 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV3;
using V4 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV4;
using V5 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV5;
using V6 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV6;
using V7 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV7;
using V8 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV8;
using V9 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV9;
using V10 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV10;
using Next = System.Net.Mqtt.Extensions.MqttExtensions;

#pragma warning disable CA1822, CA1812

namespace System.Net.Mqtt.Benchmarks.Extensions;

[SampleSetsFilter("large", "medium", "small")]
[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
public class TopicMatchingBenchmarks
{
    public static IEnumerable<FilterTopicSampleSet> Samples { get; } = new FilterTopicSampleSet[]
    {
        new("Small", new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
        {
            ("l1/l2/l3"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2"u8.ToArray()),
            ("l1/l2/"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("0l1/l2/l3"u8.ToArray(), "1l1/l2/l3"u8.ToArray()),
            ("l1/l2/0l3"u8.ToArray(), "l1/l2/1l3"u8.ToArray()),
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
            ("level1/level2/level3"u8.ToArray(), "level1/level2"u8.ToArray()),
            ("level1/level2/"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("0level1/level2/level3"u8.ToArray(), "1level1/level2/level3"u8.ToArray()),
            ("level1/level2/0level3"u8.ToArray(), "level1/level2/1level3"u8.ToArray()),
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
            ("test/0level111/level222/level33"u8.ToArray(), "+/1level111/level2/#"u8.ToArray()),
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
    public void TopicMatchesV1([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V1.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV2([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V2.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV3([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V3.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV4([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V4.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV5([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V5.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV6([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V6.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV7([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V7.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV8([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V8.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV9([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V9.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV10([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            V10.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Samples))]
    public void TopicMatchesV11([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            Next.TopicMatches(span[i].Item1.Span, span[i].Item2.Span);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CommonPrefixLength")]
    [ArgumentsSource(nameof(Samples))]
    public void CommonPrefixLengthScalar([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var left = span[i].Item1.Span;
            var right = span[i].Item2.Span;
            Next.CommonPrefixLengthScalar(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        }
    }

    [Benchmark]
    [BenchmarkCategory("CommonPrefixLength")]
    [ArgumentsSource(nameof(Samples))]
    public void CommonPrefixLengthSWAR([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var left = span[i].Item1.Span;
            var right = span[i].Item2.Span;
            Next.CommonPrefixLengthSWAR(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        }
    }

    [Benchmark]
    [BenchmarkCategory("CommonPrefixLength")]
    [ArgumentsSource(nameof(Samples))]
    public void CommonPrefixLengthSIMD([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var left = span[i].Item1.Span;
            var right = span[i].Item2.Span;
            Next.CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FirstSegmentLengthScalar")]
    [ArgumentsSource(nameof(Samples))]
    public void FirstSegmentLengthScalar([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var source = span[i].Item1.Span;
            Next.FirstSegmentLengthScalar(ref Unsafe.AsRef(in source[0]), source.Length);
        }
    }

    [Benchmark]
    [BenchmarkCategory("FirstSegmentLengthScalar")]
    [ArgumentsSource(nameof(Samples))]
    public void FirstSegmentLengthSWAR([NotNull] FilterTopicSampleSet sampleSet)
    {
        var span = sampleSet.Samples.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var source = span[i].Item1.Span;
            Next.FirstSegmentLengthSWAR(ref Unsafe.AsRef(in source[0]), source.Length);
        }
    }
}

public sealed class FilterTopicSampleSet : SampleSet<ValueTuple<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>
{
    private string? displayString;

    public FilterTopicSampleSet(string name, (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[] samples) : base(name, samples) { }

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