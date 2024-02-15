using Net.Mqtt.Packets.V3;
using ByteSequence = System.Buffers.ReadOnlySequence<byte>;
using SampleSet = Net.Mqtt.Benchmarks.SampleSet<System.ValueTuple<byte, byte, System.Buffers.ReadOnlySequence<byte>>>;
using SF = OOs.Memory.SequenceFactory;

#pragma warning disable CA1822, CA1812

namespace Net.Mqtt.Benchmarks.Packets;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
public class PublishPacketBenchmarks
{
    public static IEnumerable<SampleSet> Samples
    {
        get
        {
            yield return new SampleSet("Solid", [
                (0b110000, 7, new ByteSequence([0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04])),
                (0b110010, 9, new ByteSequence([0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04])),
                (0b110100, 9, new ByteSequence([0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04])),
                (0b111011, 14, new ByteSequence([0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03])),
                (0b111011, 14, new ByteSequence([0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03])),
                (0b111011, 14, new ByteSequence([0x00, 0x05, 0x61, 0x2f])),
                (0b111011, 14, ByteSequence.Empty)
            ]);

            yield return new SampleSet("Fragmented", [
                (0b110000, 7, SF.Create<byte>(new byte[] { 0x00, 0x05, 0x61, 0x2f }, new byte[] { 0x62, 0x2f, 0x63, 0x00 }, new byte[] { 0x04 })),
                (0b110000, 7, SF.Create<byte>(new byte[] { 0x00, 0x05 }, new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63 }, new byte[] { 0x00, 0x04 })),
                (0b110000, 7, SF.Create<byte>(new byte[] { 0x00, 0x05 }, new byte[] { 0x61, 0x2f }, new byte[] { 0x62, 0x2f }, new byte[] { 0x63, 0x00 }, new byte[] { 0x04 })),
                (0b110000, 7, SF.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x05, 0x61, 0x2f }, new byte[] { 0x62, 0x2f }, new byte[] { 0x63, 0x00 }, new byte[] { 0x04 })),
                (0b110010, 9, SF.Create<byte>(new byte[] { 0x00, 0x05 }, new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04 })),
                (0b110010, 9, SF.Create<byte>(new byte[] { 0x00, 0x05 }, new byte[] { 0x61, 0x2f, 0x62 }, new byte[] { 0x2f, 0x63, 0x00, 0x04 })),
                (0b110010, 9, SF.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x05, 0x61, 0x2f, 0x62 }, new byte[] { 0x2f, 0x63, 0x00, 0x04 })),
                (0b111011, 14, SF.Create<byte>(new byte[] { 0x00, 0x05 }, new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03 })),
                (0b111011, 14, SF.Create<byte>(new byte[] { 0x00, 0x05 }, new byte[] { 0x61, 0x2f, 0x62, 0x2f }, new byte[] { 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03 })),
                (0b111011, 14, SF.Create<byte>(new byte[] { 0x00, 0x05 }, new byte[] { 0x61, 0x2f, 0x62, 0x2f }, new byte[] { 0x63, 0x00, 0x04 }, new byte[] { 0x03, 0x04, 0x05, 0x04, 0x03 })),
                (0b111011, 14, SF.Create<byte>(new byte[] { 0x00, 0x05, 0x61, 0x2f, 0x62 }, new byte[] { 0x2f, 0x63, 0x00, 0x04, 0x03 })),
                (0b111011, 14, SF.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x05, 0x61, 0x2f })),
                (0b111011, 14, SF.Create<byte>(Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>()))]);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TryReadPayload")]
    [ArgumentsSource(nameof(Samples))]
    public void TryReadPayloadV1([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            var (header, length, sequence) = samples[i];
            PublishPacketV1.TryReadPayload(in sequence, header, length, out _, out _, out _);
        }
    }

    [Benchmark]
    [BenchmarkCategory("TryReadPayload")]
    [ArgumentsSource(nameof(Samples))]
    public void TryReadPayloadNext([NotNull] SampleSet sampleSet)
    {
        var samples = sampleSet.Samples.AsSpan();
        for (var i = 0; i < samples.Length; i++)
        {
            var (header, length, sequence) = samples[i];
            PublishPacket.TryReadPayloadExact(in sequence, length, (header >> 1 & PacketFlags.QoSMask) != 0, out _, out _, out _);
        }
    }
}