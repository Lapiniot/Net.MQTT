using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V3.UnsubscribePacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidSample()
    {
        var sequence = new ReadOnlySequence<byte>([0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65, 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69]);

        var actual = Packets.V3.UnsubscribePacket.TryReadPayload(sequence.Slice(2), 23, out var id, out var filters);

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
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f },
            new byte[] { 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65 },
            new byte[] { 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 });

        var actual = Packets.V3.UnsubscribePacket.TryReadPayload(sequence.Slice(2), 23, out var id, out var filters);

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
        var sequence = new ReadOnlySequence<byte>([0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65, 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f]);

        var actual = Packets.V3.UnsubscribePacket.TryReadPayload(sequence.Slice(2), 23, out var id, out var filters);

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
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f },
            new byte[] { 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65 },
            new byte[] { 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 },
            new byte[] { 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 });

        var actual = Packets.V3.UnsubscribePacket.TryReadPayload(sequence.Slice(2), 23, out var id, out var filters);

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
        var sequence = new ReadOnlySequence<byte>([0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65]);

        var actual = Packets.V3.UnsubscribePacket.TryReadPayload(sequence.Slice(2), 23, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUninitialized_GivenIncompleteFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b10100010, 23, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f },
            new byte[] { 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65 },
            new byte[] { 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 });

        var actual = Packets.V3.UnsubscribePacket.TryReadPayload(sequence.Slice(2, sequence.Length - 4), 23, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUninitialized_GivenEmptySample()
    {
        var actual = Packets.V3.UnsubscribePacket.TryReadPayload(ReadOnlySequence<byte>.Empty, 23, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }
}