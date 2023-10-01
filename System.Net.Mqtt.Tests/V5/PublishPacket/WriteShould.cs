using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SpanExtensions;

namespace System.Net.Mqtt.Tests.V5.PublishPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void EncodeHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        var written = new Packets.V5.PublishPacket(0, default, "TestTopic"u8.ToArray(), "TestMessage"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(0b0011_0000, actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(23, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePropsLengthZero_GivenSampleMessageWithDefaultProps()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        var written = new Packets.V5.PublishPacket(0, default, "TestTopic"u8.ToArray(), "TestMessage"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        Assert.IsTrue(TryReadMqttVarByteInteger(bytes[13..], out var propLen, out _));
        Assert.AreEqual(0, propLen);
    }

    [TestMethod]
    public void SetDuplicateFlag_GivenMessageWithDuplicateTrue()
    {
        var writer = new ArrayBufferWriter<byte>(10);
        var written = new Packets.V5.PublishPacket(0, default, "topic"u8.ToArray(), default, duplicate: true).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(10, written);
        Assert.AreEqual(10, writer.WrittenCount);

        var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
        Assert.AreEqual(PacketFlags.Duplicate, actualDuplicateValue);
    }

    [TestMethod]
    public void ResetDuplicateFlag_GivenMessageWithDuplicateFalse()
    {
        var writer = new ArrayBufferWriter<byte>(10);
        var written = new Packets.V5.PublishPacket(0, default, "topic"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(10, written);
        Assert.AreEqual(10, writer.WrittenCount);

        var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
        Assert.AreEqual(0, actualDuplicateValue);
    }

    [TestMethod]
    public void SetRetainFlag_GivenMessageWithRetainTrue()
    {
        var writer = new ArrayBufferWriter<byte>(10);
        var written = new Packets.V5.PublishPacket(0, default, "topic"u8.ToArray(), retain: true).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(10, written);
        Assert.AreEqual(10, writer.WrittenCount);

        var actualDuplicateValue = bytes[0] & PacketFlags.Retain;
        Assert.AreEqual(PacketFlags.Retain, actualDuplicateValue);
    }

    [TestMethod]
    public void ResetRetainFlag_GivenMessageWithRetainFalse()
    {
        var writer = new ArrayBufferWriter<byte>(10);
        var written = new Packets.V5.PublishPacket(0, default, "topic"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(10, written);
        Assert.AreEqual(10, writer.WrittenCount);

        var actualRetainValue = bytes[0] & PacketFlags.Retain;
        Assert.AreEqual(0, actualRetainValue);
    }

    [TestMethod]
    public void SetQoSFlag_0b00_GivenMessageWithQoSAtMostOnce()
    {
        var writer = new ArrayBufferWriter<byte>(10);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(10, written);
        Assert.AreEqual(10, writer.WrittenCount);

        var actualQoS = bytes[0] & PacketFlags.QoSLevel0;
        Assert.AreEqual(PacketFlags.QoSLevel0, actualQoS);
    }

    [TestMethod]
    public void SetQoSFlag_0b01_GivenMessageWithQoSAtLeastOnce()
    {
        var writer = new ArrayBufferWriter<byte>(12);
        var written = new Packets.V5.PublishPacket(100, 1, "topic"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(12, written);
        Assert.AreEqual(12, writer.WrittenCount);

        var actualQoS = bytes[0] & PacketFlags.QoSLevel1;
        Assert.AreEqual(PacketFlags.QoSLevel1, actualQoS);
    }

    [TestMethod]
    public void SetQoSFlag_0b10_GivenMessageWithQoSExactlyOnce()
    {
        var writer = new ArrayBufferWriter<byte>(12);
        var written = new Packets.V5.PublishPacket(100, 2, "topic"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(12, written);
        Assert.AreEqual(12, writer.WrittenCount);

        var actualQoS = bytes[0] & PacketFlags.QoSLevel2;
        Assert.AreEqual(PacketFlags.QoSLevel2, actualQoS);
    }

    [TestMethod]
    public void EncodeTopic_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        var written = new Packets.V5.PublishPacket(0, default, "TestTopic"u8.ToArray(), "TestMessage"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual(9, actualTopicLength);

        var actualTopic = bytes.Slice(4, 9);
        Assert.IsTrue(actualTopic.SequenceEqual("TestTopic"u8));
    }

    [TestMethod]
    public void EncodePayload_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        var written = new Packets.V5.PublishPacket(0, default, "TestTopic"u8.ToArray(), "TestMessage"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        var actualMessage = bytes.Slice(14, 11);
        Assert.IsTrue(actualMessage.SequenceEqual("TestMessage"u8.ToArray()));
    }

    [TestMethod]
    public void NotEncodePacketId_GivenMessageWithQoSAtMostOnce()
    {
        var writer = new ArrayBufferWriter<byte>(17);
        var written = new Packets.V5.PublishPacket(100, 0, "topic"u8.ToArray(), "message"u8.ToArray()).Write(writer, int.MaxValue);

        Assert.AreEqual(17, written);
        Assert.AreEqual(17, writer.WrittenCount);
    }

    [TestMethod]
    public void EncodePacketId_GivenMessageWithQoSAtLeastOnce()
    {

        var writer = new ArrayBufferWriter<byte>(19);
        var written = new Packets.V5.PublishPacket((ushort)100u, 1, "topic"u8.ToArray(), "message"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(19, written);
        Assert.AreEqual(19, writer.WrittenCount);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
        Assert.AreEqual(100u, actualPacketId);
    }

    [TestMethod]
    public void EncodePacketId_GivenMessageWithQoSExactlyOnce()
    {
        var writer = new ArrayBufferWriter<byte>(19);
        var written = new Packets.V5.PublishPacket((ushort)100u, 2, "topic"u8.ToArray(), "message"u8.ToArray()).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(19, written);
        Assert.AreEqual(19, writer.WrittenCount);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
        Assert.AreEqual(100u, actualPacketId);
    }

    [TestMethod]
    public void EncodePayloadFormat_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(12);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { PayloadFormat = 1 }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(12, written);
        Assert.AreEqual(12, writer.WrittenCount);

        Assert.IsTrue(bytes[9..].SequenceEqual(new byte[] { 0x02, 0x01, 0x01 }));
    }

    [TestMethod]
    public void EncodeMessageExpiryInterval_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { MessageExpiryInterval = 300 }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        Assert.IsTrue(bytes[9..].SequenceEqual(new byte[] { 0x05, 0x02, 0x00, 0x00, 0x01, 0x2c }));
    }

    [TestMethod]
    public void EncodeContentType_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(23);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { ContentType = "text/plain"u8.ToArray() }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(23, written);
        Assert.AreEqual(23, writer.WrittenCount);

        Assert.IsTrue(bytes[9..].SequenceEqual(new byte[] { 0x0d, 0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e }));
    }

    [TestMethod]
    public void EncodeResponseTopic_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(23);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { ResponseTopic = "response/1"u8.ToArray() }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(23, written);
        Assert.AreEqual(23, writer.WrittenCount);

        Assert.IsTrue(bytes[9..].SequenceEqual(new byte[] { 0x0d, 0x08, 0x00, 0x0a, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x31 }));
    }

    [TestMethod]
    public void EncodeCorrelationData_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(23);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { CorrelationData = "0123456789"u8.ToArray() }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(23, written);
        Assert.AreEqual(23, writer.WrittenCount);

        Assert.IsTrue(bytes[9..].SequenceEqual(new byte[] { 0x0d, 0x09, 0x00, 0x0a, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 }));
    }

    [TestMethod]
    public void EncodeSubscriptionId_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(19);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { SubscriptionIds = new uint[] { 265000, 1024, 42 } }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(19, written);
        Assert.AreEqual(19, writer.WrittenCount);

        Assert.IsTrue(bytes[9..].SequenceEqual(new byte[] { 0x09, 0x0b, 0xa8, 0x96, 0x10, 0x0b, 0x80, 0x08, 0x0b, 0x2a }));
    }

    [TestMethod]
    public void EncodeTopicAlias_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(13);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { TopicAlias = 30000 }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(13, written);
        Assert.AreEqual(13, writer.WrittenCount);

        Assert.IsTrue(bytes[9..].SequenceEqual(new byte[] { 0x03, 0x23, 0x75, 0x30 }));
    }

    [TestMethod]
    public void EncodeProperties_GivenMessageWithNotDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(13);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray())
        {
            UserProperties = new List<Utf8StringPair>()
            {
                new("user-prop-1"u8.ToArray(),"user-prop1-value"u8.ToArray()),
                new("user-prop-2"u8.ToArray(),"user-prop2-value"u8.ToArray())
            }
        }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(74, written);
        Assert.AreEqual(74, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual((ReadOnlySpan<byte>)([0x30, 0x48, 0x00, 0x05, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x40, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x10, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x10, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65])));
    }

    [TestMethod]
    public void WriteZeroBytes_IfSizeExceedsMaxAllowedBytes()
    {
        var writer = new ArrayBufferWriter<byte>(23);
        var written = new Packets.V5.PublishPacket(0, 0, "topic"u8.ToArray()) { ContentType = "text/plain"u8.ToArray() }
            .Write(writer, 16);

        Assert.AreEqual(0, written);
        Assert.AreEqual(0, writer.WrittenCount);
    }
}