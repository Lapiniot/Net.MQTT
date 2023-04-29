using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V3.PublishPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void EncodeHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(24);
        var written = new Packets.V3.PublishPacket(0, default, "TestTopic"u8.ToArray(), "TestMessage"u8.ToArray()).Write(writer, out var bytes);

        Assert.AreEqual(24, written);
        Assert.AreEqual(24, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(0b0011_0000, actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(22, actualRemainingLength);
    }

    [TestMethod]
    public void SetDuplicateFlag_GivenMessageWithDuplicateTrue()
    {
        var writer = new ArrayBufferWriter<byte>(9);
        var written = new Packets.V3.PublishPacket(0, default, "topic"u8.ToArray(), default, duplicate: true).Write(writer, out var bytes);

        Assert.AreEqual(9, written);
        Assert.AreEqual(9, writer.WrittenCount);

        var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
        Assert.AreEqual(PacketFlags.Duplicate, actualDuplicateValue);
    }

    [TestMethod]
    public void ResetDuplicateFlag_GivenMessageWithDuplicateFalse()
    {
        var writer = new ArrayBufferWriter<byte>(9);
        var written = new Packets.V3.PublishPacket(0, default, "topic"u8.ToArray()).Write(writer, out var bytes);

        Assert.AreEqual(9, written);
        Assert.AreEqual(9, writer.WrittenCount);

        var actualDuplicateValue = bytes[0] & PacketFlags.Duplicate;
        Assert.AreEqual(0, actualDuplicateValue);
    }

    [TestMethod]
    public void SetRetainFlag_GivenMessageWithRetainTrue()
    {
        var writer = new ArrayBufferWriter<byte>(9);
        var written = new Packets.V3.PublishPacket(0, default, "topic"u8.ToArray(), retain: true).Write(writer, out var bytes);

        Assert.AreEqual(9, written);
        Assert.AreEqual(9, writer.WrittenCount);

        var actualDuplicateValue = bytes[0] & PacketFlags.Retain;
        Assert.AreEqual(PacketFlags.Retain, actualDuplicateValue);
    }

    [TestMethod]
    public void ResetRetainFlag_GivenMessageWithRetainFalse()
    {
        var writer = new ArrayBufferWriter<byte>(9);
        var written = new Packets.V3.PublishPacket(0, default, "topic"u8.ToArray()).Write(writer, out var bytes);

        Assert.AreEqual(9, written);
        Assert.AreEqual(9, writer.WrittenCount);

        var actualRetainValue = bytes[0] & PacketFlags.Retain;
        Assert.AreEqual(0, actualRetainValue);
    }

    [TestMethod]
    public void SetQoSFlag_0b00_GivenMessageWithQoSAtMostOnce()
    {
        var writer = new ArrayBufferWriter<byte>(9);
        var written = new Packets.V3.PublishPacket(0, 0, "topic"u8.ToArray()).Write(writer, out var bytes);

        Assert.AreEqual(9, written);
        Assert.AreEqual(9, writer.WrittenCount);

        var actualQoS = bytes[0] & PacketFlags.QoSLevel0;
        Assert.AreEqual(PacketFlags.QoSLevel0, actualQoS);
    }

    [TestMethod]
    public void SetQoSFlag_0b01_GivenMessageWithQoSAtLeastOnce()
    {
        var writer = new ArrayBufferWriter<byte>(11);
        var written = new Packets.V3.PublishPacket(100, 1, "topic"u8.ToArray()).Write(writer, out var bytes);

        Assert.AreEqual(11, written);
        Assert.AreEqual(11, writer.WrittenCount);

        var actualQoS = bytes[0] & PacketFlags.QoSLevel1;
        Assert.AreEqual(PacketFlags.QoSLevel1, actualQoS);
    }

    [TestMethod]
    public void SetQoSFlag_0b10_GivenMessageWithQoSExactlyOnce()
    {
        var writer = new ArrayBufferWriter<byte>(11);
        var written = new Packets.V3.PublishPacket(100, 2, "topic"u8.ToArray()).Write(writer, out var bytes);

        Assert.AreEqual(11, written);
        Assert.AreEqual(11, writer.WrittenCount);

        var actualQoS = bytes[0] & PacketFlags.QoSLevel2;
        Assert.AreEqual(PacketFlags.QoSLevel2, actualQoS);
    }

    [TestMethod]
    public void EncodeTopic_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(24);
        var written = new Packets.V3.PublishPacket(0, default, "TestTopic"u8.ToArray(), "TestMessage"u8.ToArray()).Write(writer, out var bytes);

        Assert.AreEqual(24, written);
        Assert.AreEqual(24, writer.WrittenCount);

        var actualTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual(9, actualTopicLength);

        var actualTopic = bytes.Slice(4, 9);
        Assert.IsTrue(actualTopic.SequenceEqual("TestTopic"u8));
    }

    [TestMethod]
    public void EncodePayload_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(24);
        var message = "TestMessage"u8.ToArray();
        var written = new Packets.V3.PublishPacket(0, default, "TestTopic"u8.ToArray(), message).Write(writer, out var bytes);

        Assert.AreEqual(24, written);
        Assert.AreEqual(24, writer.WrittenCount);

        var actualMessage = bytes.Slice(bytes.Length - message.Length, message.Length);
        Assert.IsTrue(actualMessage.SequenceEqual(message));
    }

    [TestMethod]
    public void NotEncodePacketId_GivenMessageWithQoSAtMostOnce()
    {
        var topic = "topic"u8;
        var payload = new byte[] { 1, 1, 1, 1 };

        var writer = new ArrayBufferWriter<byte>(13);
        var written = new Packets.V3.PublishPacket(100, 0, topic.ToArray(), payload).Write(writer, out var bytes);

        Assert.AreEqual(13, written);
        Assert.AreEqual(13, writer.WrittenCount);

        var length = 1 + 1 + 2 + topic.Length + payload.Length;
        var actualLength = bytes.Length;
        Assert.AreEqual(length, actualLength);
    }

    [TestMethod]
    public void EncodePacketId_GivenMessageWithQoSAtLeastOnce()
    {
        var topic = "topic"u8;
        var payload = new byte[] { 1, 1, 1, 1 };
        const ushort packetId = 100;

        var writer = new ArrayBufferWriter<byte>(15);
        var written = new Packets.V3.PublishPacket(packetId, 1, topic.ToArray(), payload).Write(writer, out var bytes);

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        var length = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
        var actualLength = bytes.Length;
        Assert.AreEqual(length, actualLength);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
        Assert.AreEqual(packetId, actualPacketId);
    }

    [TestMethod]
    public void EncodePacketId_GivenMessageWithQoSExactlyOnce()
    {
        var topic = "topic"u8;
        var payload = new byte[] { 1, 1, 1, 1 };
        const ushort packetId = 100;

        var writer = new ArrayBufferWriter<byte>(15);
        var written = new Packets.V3.PublishPacket(packetId, 2, topic.ToArray(), payload).Write(writer, out var bytes);

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        var length = 1 + 1 + 2 + topic.Length + 2 /*Id bytes*/ + payload.Length;
        var actualLength = bytes.Length;
        Assert.AreEqual(length, actualLength);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[9..]);
        Assert.AreEqual(packetId, actualPacketId);
    }
}