using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using SampleSet = System.Net.Mqtt.Benchmarks.SampleSet<System.ValueTuple<System.ReadOnlyMemory<byte>, System.ReadOnlyMemory<byte>>>;
using v1 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV1;
using v2 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV2;
using v3 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV3;
using v4 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV4;
using v5 = System.Net.Mqtt.Extensions.MqttExtensions;

#pragma warning disable CA1822

namespace System.Net.Mqtt.Benchmarks.Extensions;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
public class TopicMatchingBenchmarks
{
    public static IEnumerable<SampleSet> Samples { get; } = new SampleSet[]
    {
        new("Small", new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
        {
            ("l1/l2/l3"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/l3/#"u8.ToArray()),
            ("l1/l2/l3/"u8.ToArray(), "l1/l2/l3/#"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2"u8.ToArray()),
            ("l1/l2/"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/l2/l3"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/+"u8.ToArray()),
            ("l1/l2/"u8.ToArray(), "l1/l2/+"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/l2/+"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/+/l3"u8.ToArray()),
            ("l1/l3"u8.ToArray(), "l1/+/l3"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/+/l3"u8.ToArray()),
            ("l1//l3"u8.ToArray(), "l1/+/l3"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l2/l3"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l1/l3"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l2/l3"u8.ToArray(), "+/l2/l3"u8.ToArray()),
            ("l1/l2/l3"u8.ToArray(), "l1/l2/#"u8.ToArray()),
            ("l1/l2/"u8.ToArray(), "l1/l2/#"u8.ToArray()),
            ("l1/l2"u8.ToArray(), "l1/l2/#"u8.ToArray()),
            ("l1/l3"u8.ToArray(), "l1/l2/#"u8.ToArray()),
            ("l1/l0/l2/l3"u8.ToArray(), "l1/+/l2/#"u8.ToArray()),
            ("t/l1/l2/l3"u8.ToArray(), "+/l1/l2/#"u8.ToArray()),
            ("t/l1/l2/l3"u8.ToArray(), "+/+/+/+"u8.ToArray()),
            ("t/l1/l2/l3"u8.ToArray(), "+/+/+/#"u8.ToArray()),
            ("t/l1/l2/l3"u8.ToArray(), "+/+/#"u8.ToArray()),
            ("t/l1/l2/l3"u8.ToArray(), "+/#"u8.ToArray()),
            ("t/l1/l2/l3"u8.ToArray(), "#"u8.ToArray())
        }),
        new("Medium", new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
        {
            ("level1/level2/level3/level1/level2/level3"u8.ToArray(), "level1/level2/level3/level1/level2/level3"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/level3/#"u8.ToArray()),
            ("level1/level2/level3/"u8.ToArray(), "level1/level2/level3/#"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2"u8.ToArray()),
            ("level1/level2/"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/level2/level3"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/+"u8.ToArray()),
            ("level1/level2/"u8.ToArray(), "level1/level2/+"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/level2/+"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/+/level3"u8.ToArray()),
            ("level1/level3"u8.ToArray(), "level1/+/level3"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/+/level3"u8.ToArray()),
            ("level1//level3"u8.ToArray(), "level1/+/level3"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level2/level3"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level1/level3"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level2/level3"u8.ToArray(), "+/level2/level3"u8.ToArray()),
            ("level1/level2/level3"u8.ToArray(), "level1/level2/#"u8.ToArray()),
            ("level1/level2/"u8.ToArray(), "level1/level2/#"u8.ToArray()),
            ("level1/level2"u8.ToArray(), "level1/level2/#"u8.ToArray()),
            ("level1/level3"u8.ToArray(), "level1/level2/#"u8.ToArray()),
            ("level1/level0/level2/level3"u8.ToArray(), "level1/+/level2/#"u8.ToArray()),
            ("test/level1/level2/level3"u8.ToArray(), "+/level1/level2/#"u8.ToArray()),
            ("test/level1/level2/level3"u8.ToArray(), "+/+/+/+"u8.ToArray()),
            ("test/level1/level2/level3"u8.ToArray(), "+/+/+/#"u8.ToArray()),
            ("test/level1/level2/level3"u8.ToArray(), "+/+/#"u8.ToArray()),
            ("test/level1/level2/level3"u8.ToArray(), "+/#"u8.ToArray()),
            ("test/level1/level2/level3"u8.ToArray(), "#"u8.ToArray())
        }),
        new("Large", new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
        {
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/testtopiclevel6"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/testtopiclevel4/testtopiclevel5/testtopiclevel6"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3/"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/+"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/"u8.ToArray(), "testtopiclevel1/testtopiclevel2/+"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "testtopiclevel1/testtopiclevel2/+"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/+/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel3"u8.ToArray(), "testtopiclevel1/+/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "testtopiclevel1/+/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1//testtopiclevel3"u8.ToArray(), "testtopiclevel1/+/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("/testtopiclevel2/testtopiclevel3"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel3"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel2/testtopiclevel3"u8.ToArray(), "+/testtopiclevel2/testtopiclevel3"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2/"u8.ToArray(), "testtopiclevel1/testtopiclevel2/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel2"u8.ToArray(), "testtopiclevel1/testtopiclevel2/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/#"u8.ToArray()),
            ("testtopiclevel1/testtopiclevel0/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/+/testtopiclevel2/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/testtopiclevel1/testtopiclevel2/#"u8.ToArray()),
            ("test/testtopiclevel111/testtopiclevel222/testtopiclevel333"u8.ToArray(), "+/+/+/+"u8.ToArray()),
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
}