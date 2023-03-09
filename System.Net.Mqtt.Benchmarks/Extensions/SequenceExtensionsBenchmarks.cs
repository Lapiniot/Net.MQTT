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
            yield return new SampleSet("Solid", new ByteSequence[] {
                new ByteSequence(),
                new ByteSequence(new byte[] { 0x1 }),
                new ByteSequence(new byte[] { 0x1, 0x2 }),
                new ByteSequence(new byte[] { 0x1, 0x2, 0x3 }) });

            yield return new SampleSet("Fragmented", new ByteSequence[] {
                SF.Create<byte>(Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1 }, Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1 }, new byte[] { 0x2 }),
                SF.Create<byte>(new byte[] { 0x1 }, new byte[] { 0x2 }, Array.Empty<byte>()),
                SF.Create<byte>(new byte[] { 0x1 }, new byte[] { 0x2 }, new byte[] { 0x3 }),
                SF.Create<byte>(new byte[] { 0x1, 0x2, 0x3 }, Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1, 0x2, 0x3 }, Array.Empty<byte>()),
                SF.Create<byte>(Array.Empty<byte>(), new byte[] { 0x1 }, Array.Empty<byte>(), new byte[] { 0x2 }, Array.Empty<byte>()) });
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
            SequenceExtensionsV1.TryReadBigEndian(samples[i], out _);
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
            Mqtt.Extensions.SequenceExtensions.TryReadBigEndian(samples[i], out _);
        }
    }
}