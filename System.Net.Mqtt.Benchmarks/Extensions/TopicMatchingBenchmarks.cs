using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using v1 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV1;
using v2 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV2;
using v3 = System.Net.Mqtt.Extensions.MqttExtensions;

#pragma warning disable CA1822

namespace System.Net.Mqtt.Benchmarks.Extensions;

public class TopicMatchingBenchmarks
{
#pragma warning disable CA1819
    public static object[] LargeSamples { get; } = new[]{ new (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)[]
    {
        ("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray()),
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
        ("test/testtopiclevel1/testtopiclevel2/testtopiclevel3"u8.ToArray(), "+/testtopiclevel1/testtopiclevel2/#"u8.ToArray()),
        ("test/testtopiclevel1/testlevel0/testtopiclevel2/testtest/testtopiclevel3"u8.ToArray(), "+/testtopiclevel1/+/testtopiclevel2/+/#"u8.ToArray())
    }};
#pragma warning restore CA1819

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(LargeSamples))]
    public void TopicMatchesV1([NotNull] (ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Filter)[] samples)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            v1.TopicMatches(samples[i].Topic.Span, samples[i].Filter.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(LargeSamples))]
    public void TopicMatchesV2([NotNull] (ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Filter)[] samples)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            v2.TopicMatches(samples[i].Topic.Span, samples[i].Filter.Span);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(LargeSamples))]
    public void TopicMatchesV3([NotNull] (ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Filter)[] samples)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            v3.TopicMatches(samples[i].Topic.Span, samples[i].Filter.Span);
        }
    }
}