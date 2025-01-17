using ByteSequence = System.Buffers.ReadOnlySequence<byte>;

namespace Net.Mqtt.Tests.V3.PublishPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_NotDecodePacketId_GivenSampleQoS0()
    {
        var sequence = new ByteSequence([0b110000, 7, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04]);

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 7, false, out var id, out _, out _);

        Assert.IsTrue(actualResult);
        Assert.AreEqual(0x00, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS1()
    {
        var sequence = new ByteSequence([0b110010, 9, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04]);

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 9, true, out var id, out _, out _);

        Assert.IsTrue(actualResult);
        Assert.AreEqual(0x04, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS2()
    {
        var sequence = new ByteSequence([0b110100, 9, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04]);

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 9, true, out var id, out _, out _);

        Assert.IsTrue(actualResult);
        Assert.AreEqual(0x04, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenСontiguouseSample()
    {
        var sequence = new ByteSequence([0b111011, 14, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03]);

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 14, true, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);
        Assert.IsTrue(topic.Span.SequenceEqual("a/b/c"u8));
        Assert.AreEqual(5, payload.Length);
        Assert.IsTrue(payload.Span.SequenceEqual(new byte[] { 0x03, 0x04, 0x05, 0x04, 0x03 }));
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenСontiguousLargeSample()
    {
        var sequence = new ByteSequence([0b111011, 14, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03, 0x00, 0x05, 0x01]);

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 14, true, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);
        Assert.IsTrue(topic.Span.SequenceEqual("a/b/c"u8));
        Assert.AreEqual(5, payload.Length);
        Assert.IsTrue(payload.Span.SequenceEqual(new byte[] { 0x03, 0x04, 0x05, 0x04, 0x03 }));
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b111011, 14, 0x00, 0x05 },
            new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 14, true, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);
        Assert.IsTrue(topic.Span.SequenceEqual("a/b/c"u8));
        Assert.AreEqual(5, payload.Length);
        Assert.IsTrue(payload.Span.SequenceEqual(new byte[] { 0x03, 0x04, 0x05, 0x04, 0x03 }));
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenFragmentedLargeSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b111011, 14, 0x00, 0x05 },
            new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03 },
            new byte[] { 0x00, 0x05, 0x01 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 14, true, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(topic.Span.SequenceEqual("a/b/c"u8));
        Assert.AreEqual(5, payload.Length);
        Assert.IsTrue(payload.Span.SequenceEqual(new byte[] { 0x03, 0x04, 0x05, 0x04, 0x03 }));
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenСontiguouseIncompleteSample()
    {
        var sequence = new ByteSequence([0b111011, 14, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03]);

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 14, true, out var id, out var topic, out var payload);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.IsTrue(topic.IsEmpty);
        Assert.IsTrue(payload.IsEmpty);
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenFragmentedIncompleteSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b111011, 14, 0x00, 0x05 },
            new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayloadExact(sequence.Slice(2), 14, true, out var id, out var topic, out var payload);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.IsTrue(topic.IsEmpty);
        Assert.IsTrue(payload.IsEmpty);
    }
}