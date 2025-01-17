using System.Buffers.Binary;

namespace Net.Mqtt.Tests.V3.UnsubscribePacket;

[TestClass]
public class WriteShould
{
    private readonly Packets.V3.UnsubscribePacket samplePacket = new(2, ["a/b/c"u8.ToArray(), "d/e/f"u8.ToArray(), "g/h/i"u8.ToArray()]);

    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual((byte)(0b1010_0000 | 0b0010), actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(23, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePacketId_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual((byte)0x0002, actualPacketId);
    }

    [TestMethod]
    public void EncodeTopics_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        var topic = "a/b/c"u8;
        var topicLength = topic.Length;

        var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]);
        Assert.AreEqual(topicLength, actualTopicLength);

        var actualTopic = bytes.Slice(6, topicLength);
        Assert.IsTrue(actualTopic.SequenceEqual(topic));

        topic = "d/e/f"u8;
        topicLength = topic.Length;

        actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[11..]);
        Assert.AreEqual(topicLength, actualTopicLength);

        actualTopic = bytes.Slice(13, topicLength);
        Assert.IsTrue(actualTopic.SequenceEqual(topic));

        topic = "g/h/i"u8;
        topicLength = topic.Length;

        actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[18..]);
        Assert.AreEqual(topicLength, actualTopicLength);

        actualTopic = bytes.Slice(20, topicLength);
        Assert.IsTrue(actualTopic.SequenceEqual(topic));
    }
}