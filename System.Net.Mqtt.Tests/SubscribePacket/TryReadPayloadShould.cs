using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubscribePacket;

[TestClass]
public class TryReadPayloadShould
{
    private readonly ReadOnlySequence<byte> emptySample = ReadOnlySequence<byte>.Empty;

    private readonly ReadOnlySequence<byte> fragmentedSample;

    private readonly ReadOnlySequence<byte> incompleteSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f
    }));

    private readonly ReadOnlySequence<byte> largerBufferSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
        0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
        0x68, 0x2f, 0x69, 0x00, 0x00, 0x05, 0x67, 0x2f,
        0x68, 0x2f, 0x69, 0x00
    }));

    private readonly ReadOnlySequence<byte> largerFragmentedSample;

    private readonly ReadOnlySequence<byte> sample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
        0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
        0x68, 0x2f, 0x69, 0x00
    }));

    public TryReadPayloadShould()
    {
        var segment1 = new MemorySegment<byte>(new byte[] { 0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f });

        var segment2 = segment1
            .Append(new byte[] { 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f })
            .Append(new byte[] { 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f })
            .Append(new byte[] { 0x68, 0x2f, 0x69, 0x00 });

        var segment3 = segment2.Append(new byte[] { 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x00 });

        fragmentedSample = new(segment1, 0, segment2, 4);
        largerFragmentedSample = new(segment1, 0, segment3, 8);
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidSample()
    {
        var actual = Packets.SubscribePacket.TryReadPayload(sample.Slice(2), 26, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Item1.AsSpan().SequenceEqual("a/b/c"u8));
        Assert.AreEqual(2, filters[0].Item2);
        Assert.IsTrue(filters[1].Item1.AsSpan().SequenceEqual("d/e/f"u8));
        Assert.AreEqual(1, filters[1].Item2);
        Assert.IsTrue(filters[2].Item1.AsSpan().SequenceEqual("g/h/i"u8));
        Assert.AreEqual(0, filters[2].Item2);
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidFragmentedSample()
    {
        var actual = Packets.SubscribePacket.TryReadPayload(fragmentedSample.Slice(2), 26, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Item1.AsSpan().SequenceEqual("a/b/c"u8));
        Assert.AreEqual(2, filters[0].Item2);
        Assert.IsTrue(filters[1].Item1.AsSpan().SequenceEqual("d/e/f"u8));
        Assert.AreEqual(1, filters[1].Item2);
        Assert.IsTrue(filters[2].Item1.AsSpan().SequenceEqual("g/h/i"u8));
        Assert.AreEqual(0, filters[2].Item2);
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidLargerBufferSample()
    {
        var actual = Packets.SubscribePacket.TryReadPayload(largerBufferSample.Slice(2), 26, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Item1.AsSpan().SequenceEqual("a/b/c"u8));
        Assert.AreEqual(2, filters[0].Item2);
        Assert.IsTrue(filters[1].Item1.AsSpan().SequenceEqual("d/e/f"u8));
        Assert.AreEqual(1, filters[1].Item2);
        Assert.IsTrue(filters[2].Item1.AsSpan().SequenceEqual("g/h/i"u8));
        Assert.AreEqual(0, filters[2].Item2);
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidLargerBufferFragmentedSample()
    {
        var actual = Packets.SubscribePacket.TryReadPayload(largerFragmentedSample.Slice(2), 26, out var id, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x2, id);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Item1.AsSpan().SequenceEqual("a/b/c"u8));
        Assert.AreEqual(2, filters[0].Item2);
        Assert.IsTrue(filters[1].Item1.AsSpan().SequenceEqual("d/e/f"u8));
        Assert.AreEqual(1, filters[1].Item2);
        Assert.IsTrue(filters[2].Item1.AsSpan().SequenceEqual("g/h/i"u8));
        Assert.AreEqual(0, filters[2].Item2);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenIncompleteSample()
    {
        var actual = Packets.SubscribePacket.TryReadPayload(incompleteSample.Slice(2), 26, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenIncompleteFragmentedSample()
    {
        var actual = Packets.SubscribePacket.TryReadPayload(fragmentedSample.Slice(2, fragmentedSample.Length - 4), 26, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenEmptyBufferSample()
    {
        var actual = Packets.SubscribePacket.TryReadPayload(emptySample, 26, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }
}