using ByteSequence = System.Buffers.ReadOnlySequence<byte>;
using SampleSet = System.Net.Mqtt.Benchmarks.SampleSet<System.Buffers.ReadOnlySequence<byte>>;
using SF = System.Memory.SequenceFactory;

#pragma warning disable CA1822, CA1812

namespace System.Net.Mqtt.Benchmarks.Extensions;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
public class SequenceExtensionsBenchmarks
{
    public static IEnumerable<SampleSet> Samples
    {
        get
        {
            yield return new SampleSet("Solid", [
                new ByteSequence(),
                new ByteSequence([0x1]),
                new ByteSequence([0x1, 0x2]),
                new ByteSequence([0x1, 0x2, 0x3])]);

            yield return new SampleSet("Fragmented", [
                SF.Create<byte>(Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1 }, Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1 }, new byte[] { 0x2 }),
                SF.Create<byte>(new byte[] { 0x1 }, new byte[] { 0x2 }, Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 0x1 }, new byte[] { 0x2 }, new byte[] { 0x3 }),
                SF.Create<byte>(new byte[] { 0x1, 0x2, 0x3 }, Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1, 0x2, 0x3 }, Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1 }, Array.Empty<byte>(), new byte[] { 0x2 }, Array.Empty<byte>())]);
        }
    }

    public static IEnumerable<SampleSet> MqttStringSamples
    {
        get
        {
            yield return new SampleSet("Solid", [
                ByteSequence.Empty,
                new ByteSequence([0x00]),
                new ByteSequence([0x00, 0x13]),
                new ByteSequence([0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0]),
                new ByteSequence([0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0, 0xb3, 0xd0, 0xb4, 0xd0, 0xb5])]);

            yield return new SampleSet("Fragmented", [
                SF.Create<byte>(Array.Empty<byte>(), Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 0x00 }, Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 0x00, 0x13 }, Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 }, Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x13 }),
                SF.Create<byte>(new byte[] { 0x00 }, Array.Empty<byte>(), new byte[] { 0x13 }),
                SF.Create<byte>(
                    new byte[] { 0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 },
                    new byte[] { 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0 },
                    new byte[] { 0xb3, 0xd0, 0xb4, 0xd0, 0xb5 }),
                SF.Create<byte>(
                    new byte[] { 0x00, 0x13 },
                    new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 },
                    new byte[] { 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0 },
                    new byte[] { 0xb3, 0xd0, 0xb4, 0xd0, 0xb5 }),
                SF.Create<byte>(
                    new byte[] { 0x00 },
                    new byte[] { 0x13 },
                    new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 },
                    new byte[] { 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0 },
                    new byte[] { 0xb3, 0xd0, 0xb4, 0xd0, 0xb5 }),
                SF.Create<byte>(
                    new byte[] { 0x00 },
                    Array.Empty<byte>(),
                    new byte[] { 0x13 },
                    new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 },
                    new byte[] { 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0, 0xb3, 0xd0, 0xb4, 0xd0, 0xb5 })]);
        }
    }

    public static IEnumerable<SampleSet> MqttHeaderSamples
    {
        get
        {
            yield return new SampleSet("Solid", [
                ByteSequence.Empty,
                new ByteSequence([64]),
                new ByteSequence([64, 205]),
                new ByteSequence([64, 205, 255]),
                new ByteSequence([64, 205, 255, 255]),
                new ByteSequence([64, 205, 255, 255, 255, 127, 0]),
                new ByteSequence([64, 205, 255, 255, 127, 0, 0])]);

            yield return new SampleSet("Fragmented", [
                SF.Create<byte>(Array.Empty<byte>(), Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 64 }, Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 64, 205 }, Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255 }),
                SF.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }),
                SF.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 255, 127, 0 }),
                SF.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 })]);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TryReadBigEndian")]
    [ArgumentsSource(nameof(Samples))]
    public void TryReadBigEndianV1([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            SequenceExtensionsV1.TryReadBigEndian(in samples[i], out _);
        }
    }

    [Benchmark]
    [BenchmarkCategory("TryReadBigEndian")]
    [ArgumentsSource(nameof(Samples))]
    public void TryReadBigEndianNext([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            Mqtt.Extensions.SequenceExtensions.TryReadBigEndian(in samples[i], out _);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TryRead")]
    [ArgumentsSource(nameof(Samples))]
    public void TryReadV1([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            SequenceExtensionsV1.TryRead(in samples[i], out _);
        }
    }

    [Benchmark]
    [BenchmarkCategory("TryRead")]
    [ArgumentsSource(nameof(Samples))]
    public void TryReadNext([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            Mqtt.Extensions.SequenceExtensions.TryRead(in samples[i], out _);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TryReadMqttString")]
    [ArgumentsSource(nameof(MqttStringSamples))]
    public void TryReadMqttStringV1([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            SequenceExtensionsV1.TryReadMqttString(in samples[i], out _, out _);
        }
    }

    [Benchmark]
    [BenchmarkCategory("TryReadMqttString")]
    [ArgumentsSource(nameof(MqttStringSamples))]
    public void TryReadMqttStringNext([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            Mqtt.Extensions.SequenceExtensions.TryReadMqttString(in samples[i], out _, out _);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TryReadMqttHeader")]
    [ArgumentsSource(nameof(MqttHeaderSamples))]
    public void TryReadMqttHeaderV1([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            SequenceExtensionsV1.TryReadMqttHeader(in samples[i], out _, out _, out _);
        }
    }

    [Benchmark]
    [BenchmarkCategory("TryReadMqttHeader")]
    [ArgumentsSource(nameof(MqttHeaderSamples))]
    public void TryReadMqttHeaderNext([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            Mqtt.Extensions.SequenceExtensions.TryReadMqttHeader(in samples[i], out _, out _, out _);
        }
    }
}