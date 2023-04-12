using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteSequence = System.Buffers.ReadOnlySequence<byte>;

namespace System.Net.Mqtt.Tests.PublishPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_NotDecodePacketId_GivenSampleQoS0()
    {
        var sequence = new ByteSequence(new byte[] { 0b110000, 7, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), false, 7, out var id, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x00, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS1()
    {
        var sequence = new ByteSequence(new byte[] { 0b110010, 9, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 9, out var id, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x04, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS2()
    {
        var sequence = new ByteSequence(new byte[] { 0b110100, 9, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 9, out var id, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x04, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenСontiguouseSample()
    {
        var sequence = new ByteSequence(new byte[] {
            0b111011, 14, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f,
            0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(topic.AsSpan().SequenceEqual("a/b/c"u8));

        Assert.AreEqual(5, payload.Length);

        Assert.AreEqual(0x03, payload[0]);
        Assert.AreEqual(0x04, payload[1]);
        Assert.AreEqual(0x05, payload[2]);
        Assert.AreEqual(0x04, payload[3]);
        Assert.AreEqual(0x03, payload[4]);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenСontiguousLargeSample()
    {
        var sequence = new ByteSequence(new byte[] {
            0b111011, 14, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00,
            0x04, 0x03, 0x04, 0x05, 0x04, 0x03, 0x00, 0x05, 0x01 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(topic.AsSpan().SequenceEqual("a/b/c"u8));

        Assert.AreEqual(5, payload.Length);

        Assert.AreEqual(0x03, payload[0]);
        Assert.AreEqual(0x04, payload[1]);
        Assert.AreEqual(0x05, payload[2]);
        Assert.AreEqual(0x04, payload[3]);
        Assert.AreEqual(0x03, payload[4]);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b111011, 14, 0x00, 0x05 },
            new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(topic.AsSpan().SequenceEqual("a/b/c"u8));

        Assert.AreEqual(5, payload.Length);

        Assert.AreEqual(0x03, payload[0]);
        Assert.AreEqual(0x04, payload[1]);
        Assert.AreEqual(0x05, payload[2]);
        Assert.AreEqual(0x04, payload[3]);
        Assert.AreEqual(0x03, payload[4]);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenFragmentedLargeSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b111011, 14, 0x00, 0x05 },
            new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04, 0x05, 0x04, 0x03 },
            new byte[] { 0x00, 0x05, 0x01 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out _, out var topic, out var payload);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(topic.AsSpan().SequenceEqual("a/b/c"u8));

        Assert.AreEqual(5, payload.Length);

        Assert.AreEqual(0x03, payload[0]);
        Assert.AreEqual(0x04, payload[1]);
        Assert.AreEqual(0x05, payload[2]);
        Assert.AreEqual(0x04, payload[3]);
        Assert.AreEqual(0x03, payload[4]);
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenСontiguouseIncompleteSample()
    {
        var sequence = new ByteSequence(new byte[] { 0b111011, 14, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out var id, out var topic, out var payload);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.AreEqual(null, topic);
        Assert.AreEqual(null, payload);
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenFragmentedIncompleteSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b111011, 14, 0x00, 0x05 },
            new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04 });

        var actualResult = Packets.V3.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out var id, out var topic, out var payload);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.AreEqual(null, topic);
        Assert.AreEqual(null, payload);
    }
}