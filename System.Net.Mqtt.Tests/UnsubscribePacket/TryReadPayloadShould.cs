using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.UnsubscribePacket;

[TestClass]
public class TryReadPayloadShould
{
    private readonly ReadOnlySequence<byte> emptySample = ReadOnlySequence<byte>.Empty;

    private readonly ReadOnlySequence<byte> fragmentedSample;

    private readonly ReadOnlySequence<byte> incompleteSample = new(new byte[]
    {
        0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65
    });

    private readonly ReadOnlySequence<byte> largerBufferSample = new(new byte[]
    {
        0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
        0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
        0x69, 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68,
        0x2f
    });

    private readonly ReadOnlySequence<byte> largerFragmentedSample;

    private readonly ReadOnlySequence<byte> sample = new(new byte[]
    {
        0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
        0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
        0x69
    });

    public TryReadPayloadShould()
    {
        var segment1 = new Segment<byte>(new byte[] { 0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f });

        var segment2 = segment1
            .Append(new byte[] { 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65 })
            .Append(new byte[] { 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 });

        fragmentedSample = new(segment1, 0, segment2, 9);

        var segment3 = segment2.Append(new byte[] { 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 });
        largerFragmentedSample = new(segment1, 0, segment3, 7);
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidSample()
    {
        var actual = Packets.UnsubscribePacket.TryReadPayload(sample.Slice(2), 23, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].AsSpan().SequenceEqual("a/b/c"u8));
        Assert.IsTrue(filters[1].AsSpan().SequenceEqual("d/e/f"u8));
        Assert.IsTrue(filters[2].AsSpan().SequenceEqual("g/h/i"u8));
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidFragmentedSample()
    {
        var actual = Packets.UnsubscribePacket.TryReadPayload(fragmentedSample.Slice(2), 23, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].AsSpan().SequenceEqual("a/b/c"u8));
        Assert.IsTrue(filters[1].AsSpan().SequenceEqual("d/e/f"u8));
        Assert.IsTrue(filters[2].AsSpan().SequenceEqual("g/h/i"u8));
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidLargerBufferSample()
    {
        var actual = Packets.UnsubscribePacket.TryReadPayload(largerBufferSample.Slice(2), 23, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].AsSpan().SequenceEqual("a/b/c"u8));
        Assert.IsTrue(filters[1].AsSpan().SequenceEqual("d/e/f"u8));
        Assert.IsTrue(filters[2].AsSpan().SequenceEqual("g/h/i"u8));
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidLargerBufferFragmentedSample()
    {
        var actual = Packets.UnsubscribePacket.TryReadPayload(largerFragmentedSample.Slice(2), 23, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].AsSpan().SequenceEqual("a/b/c"u8));
        Assert.IsTrue(filters[1].AsSpan().SequenceEqual("d/e/f"u8));
        Assert.IsTrue(filters[2].AsSpan().SequenceEqual("g/h/i"u8));
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUninitialized_GivenIncompleteSample()
    {
        var actual = Packets.UnsubscribePacket.TryReadPayload(incompleteSample.Slice(2), 23, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUninitialized_GivenIncompleteFragmentedSample()
    {
        var actual = Packets.UnsubscribePacket.TryReadPayload(fragmentedSample.Slice(2, fragmentedSample.Length - 4), 23, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUninitialized_GivenEmptySample()
    {
        var actual = Packets.UnsubscribePacket.TryReadPayload(emptySample, 23, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }
}