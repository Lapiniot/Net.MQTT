using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V3.UnsubscribePacket;

[TestClass]
public class WriteShould
{
    private readonly Packets.V3.UnsubscribePacket samplePacket = new(2, new ReadOnlyMemory<byte>[] { "a/b/c"u8.ToArray(), "d/e/f"u8.ToArray(), "g/h/i"u8.ToArray() });

    [TestMethod]
    public void SetHeaderBytes0Xa20X17GivenSampleMessage()
    {
        var bytes = new byte[25];
        samplePacket.Write(bytes, 23);

        const byte expectedHeaderFlags = 0b1010_0000 | 0b0010;
        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

        const int expectedRemainingLength = 23;
        var actualRemainingLength = bytes[1];
        Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePacketId0X0002GivenSampleMessage()
    {
        Span<byte> bytes = new byte[25];
        samplePacket.Write(bytes, 23);

        const byte expectedPacketId = 0x0002;
        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual(expectedPacketId, actualPacketId);
    }

    [TestMethod]
    public void EncodeTopicsGivenSampleMessage()
    {
        Span<byte> bytes = new byte[25];
        samplePacket.Write(bytes, 23);

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