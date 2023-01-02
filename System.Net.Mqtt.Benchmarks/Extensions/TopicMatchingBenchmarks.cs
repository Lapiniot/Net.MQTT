using BenchmarkDotNet.Attributes;
using v1 = System.Net.Mqtt.Benchmarks.Extensions.MqttExtensionsV1;
using v2 = System.Net.Mqtt.Extensions.MqttExtensions;

#pragma warning disable CA1822

namespace System.Net.Mqtt.Benchmarks.Extensions;

public class TopicMatchingBenchmarks
{
    [Benchmark(Baseline = true)]
    public void TopicMatchesV1()
    {
        v1.TopicMatches("testtopic/testtopic/testtopic"u8, "testtopic/testtopic/#"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopic"u8, "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/testtopiclevel2/#"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/#"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "#"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/testtopiclevel2/+"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/+/testtopiclevel3"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/testtopiclevel2/testtopiclevel3"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/testtopiclevel2/+"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/+/testtopiclevel3"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/+/+"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/+/#"u8);
        v1.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/+/#"u8);
    }

    [Benchmark]
    public void TopicMatchesCurrent()
    {
        v2.TopicMatches("testtopic/testtopic/testtopic"u8, "testtopic/testtopic/#"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopic"u8, "testtopiclevel1/testtopiclevel2/testtopiclevel3"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/testtopiclevel2/#"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/#"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "#"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/testtopiclevel2/+"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/+/testtopiclevel3"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/testtopiclevel2/testtopiclevel3"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/testtopiclevel2/+"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/+/testtopiclevel3"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/+/+"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "testtopiclevel1/+/#"u8);
        v2.TopicMatches("testtopiclevel1/testtopiclevel2/testtopiclevel3"u8, "+/+/#"u8);
    }
}