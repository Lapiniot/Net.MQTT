using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubscribePacket;

[TestClass]
public class WriteShould
{
    private readonly Packets.SubscribePacket samplePacket = new(2, new (ReadOnlyMemory<byte>, byte)[] { ("a/b/c"u8.ToArray(), 2), ("d/e/f"u8.ToArray(), 1), ("g/h/i"u8.ToArray(), 0) });

    [TestMethod]
    public void SetHeaderBytes0X820X1AGivenSampleMessage()
    {
        Span<byte> bytes = new byte[28];
        samplePacket.Write(bytes, 26);

        const byte expectedHeaderFlags = 0b1000_0000 | 0b0010;
        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

        const int expectedRemainingLength = 0x1a;
        var actualRemainingLength = bytes[1];
        Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePacketId0X0002GivenSampleMessage()
    {
        Span<byte> bytes = new byte[28];
        samplePacket.Write(bytes, 26);

        const byte expectedPacketId = 0x0002;
        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual(expectedPacketId, actualPacketId);
    }

    [TestMethod]
    public void EncodeTopicsWithQoSGivenSampleMessage()
    {
        Span<byte> bytes = new byte[28];
        samplePacket.Write(bytes, 26);

        var topic = "a/b/c"u8;
        var topicLength = topic.Length;
        var qoS = 2;

        var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]);
        Assert.AreEqual(topicLength, actualTopicLength);

        var actualTopic = bytes.Slice(6, topicLength);
        Assert.IsTrue(actualTopic.SequenceEqual(topic));

        var actualQoS = bytes[11];
        Assert.AreEqual(qoS, actualQoS);

        topic = "d/e/f"u8;
        topicLength = topic.Length;
        qoS = 1;

        actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[12..]);
        Assert.AreEqual(topicLength, actualTopicLength);

        actualTopic = bytes.Slice(14, topicLength);
        Assert.IsTrue(actualTopic.SequenceEqual(topic));

        actualQoS = bytes[19];
        Assert.AreEqual(qoS, actualQoS);

        topic = "g/h/i"u8;
        topicLength = topic.Length;
        qoS = 0;

        actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[20..]);
        Assert.AreEqual(topicLength, actualTopicLength);

        actualTopic = bytes.Slice(22, topicLength);
        Assert.IsTrue(actualTopic.SequenceEqual(topic));

        actualQoS = bytes[27];
        Assert.AreEqual(qoS, actualQoS);
    }
}