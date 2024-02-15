using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V3.SubscribePacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidSample()
    {
        var sequence = new ReadOnlySequence<byte>([0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f, 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x00]);

        var actual = Packets.V3.SubscribePacket.TryReadPayload(sequence.Slice(2), 26, out var id, out var filters);

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
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f },
            new byte[] { 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f },
            new byte[] { 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f },
            new byte[] { 0x68, 0x2f, 0x69, 0x00 });

        var actual = Packets.V3.SubscribePacket.TryReadPayload(sequence.Slice(2), 26, out var id, out var filters);

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
        var sequence = new ReadOnlySequence<byte>([0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f, 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x00, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x00]);

        var actual = Packets.V3.SubscribePacket.TryReadPayload(sequence.Slice(2), 26, out var id, out var filters);

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
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f },
            new byte[] { 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f },
            new byte[] { 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f },
            new byte[] { 0x68, 0x2f, 0x69, 0x00 },
            new byte[] { 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x00 });

        var actual = Packets.V3.SubscribePacket.TryReadPayload(sequence.Slice(2), 26, out var id, out var filters);

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
        var sequence = new ReadOnlySequence<byte>([0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f]);

        var actual = Packets.V3.SubscribePacket.TryReadPayload(sequence.Slice(2), 26, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenIncompleteFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b10000010, 26, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f },
            new byte[] { 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f },
            new byte[] { 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f },
            new byte[] { 0x68, 0x2f, 0x69, 0x00 });

        var actual = Packets.V3.SubscribePacket.TryReadPayload(sequence.Slice(2, sequence.Length - 4), 26, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenEmptyBufferSample()
    {
        var actual = Packets.V3.SubscribePacket.TryReadPayload(ReadOnlySequence<byte>.Empty, 26, out var id, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.IsNull(filters);
    }
}