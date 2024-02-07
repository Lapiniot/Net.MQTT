using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V3.ConnectPacket;

[TestClass]
public class V4WriteShould
{
    private readonly Packets.V3.ConnectPacket samplePacket =
        new("TestClientId"u8.ToArray(), 0x04, "MQTT"u8.ToArray(), 120, true,
            "TestUser"u8.ToArray(), "TestPassword"u8.ToArray(),
            "TestWillTopic"u8.ToArray(), "TestWillMessage"u8.ToArray());

    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(82);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(82, written);
        Assert.AreEqual(82, writer.WrittenCount);

        var actualPacketType = bytes[0];
        Assert.AreEqual((byte)0b0001_0000, actualPacketType);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(80, actualRemainingLength);
    }

    [TestMethod]
    public void SetProtocolInfoBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(82);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(82, written);
        Assert.AreEqual(82, writer.WrittenCount);

        var actualProtocolNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual(4, actualProtocolNameLength);

        var actualProtocolName = bytes.Slice(4, 4);
        Assert.IsTrue(actualProtocolName.SequenceEqual("MQTT"u8));

        var actualProtocolVersion = bytes[8];
        Assert.AreEqual(0x4, actualProtocolVersion);
    }

    [TestMethod]
    public void SetKeepAliveBytes_GivenMessageWithKeepAlive()
    {
        var writer = new ArrayBufferWriter<byte>(14);
        var written = new Packets.V3.ConnectPacket(null, 0x04, "MQTT"u8.ToArray(), 3600).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(14, written);
        Assert.AreEqual(14, writer.WrittenCount);

        Assert.AreEqual(0x0e, bytes[10]);
        Assert.AreEqual(0x10, bytes[11]);
    }

    [TestMethod]
    public void EncodeClientId_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(82);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(82, written);
        Assert.AreEqual(82, writer.WrittenCount);

        var actualClientIdLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[12..]);
        Assert.AreEqual(12, actualClientIdLength);

        var actualClientId = bytes.Slice(14, 12);
        Assert.IsTrue(actualClientId.SequenceEqual("TestClientId"u8));
    }

    [TestMethod]
    public void EncodeWillTopic_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(82);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(82, written);
        Assert.AreEqual(82, writer.WrittenCount);

        var actualWillTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[26..]);
        Assert.AreEqual(13, actualWillTopicLength);

        var actualWillTopic = bytes.Slice(28, 13);
        Assert.IsTrue(actualWillTopic.SequenceEqual("TestWillTopic"u8));
    }

    [TestMethod]
    public void EncodeWillMessage_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(82);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(82, written);
        Assert.AreEqual(82, writer.WrittenCount);

        var actualWillMessageLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[41..]);
        Assert.AreEqual(15, actualWillMessageLength);

        var actualWillMessage = bytes.Slice(43, 15);
        Assert.IsTrue(actualWillMessage.SequenceEqual("TestWillMessage"u8));
    }

    [TestMethod]
    public void EncodeUserName_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(82);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(82, written);
        Assert.AreEqual(82, writer.WrittenCount);

        var actualUserNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[58..]);
        Assert.AreEqual(8, actualUserNameLength);

        var actualUserName = bytes.Slice(60, 8);
        Assert.IsTrue(actualUserName.SequenceEqual("TestUser"u8));
    }

    [TestMethod]
    public void EncodePassword_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(82);
        var written = samplePacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(82, written);
        Assert.AreEqual(82, writer.WrittenCount);

        var actualPasswordLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[68..]);
        Assert.AreEqual(12, actualPasswordLength);

        var actualPassword = bytes.Slice(70, 12);
        Assert.IsTrue(actualPassword.SequenceEqual("TestPassword"u8));
    }

    [TestMethod]
    public void SetCleanSessionFlag_GivenMessageWithCleanSessionTrue()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0000_0010;
        Assert.AreEqual(0b0000_0010, actual);
    }

    [TestMethod]
    public void ResetCleanSessionFlag_GivenMessageWithCleanSessionFalse()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(), cleanSession: false).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0000_0010;
        Assert.AreEqual(0b0000_0000, actual);
    }

    [TestMethod]
    public void SetLastWillRetainFlag_GivenMessageWithLastWillRetainTrue()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(), willRetain: true).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0010_0000;
        Assert.AreEqual(0b0010_0000, actual);
    }

    [TestMethod]
    public void ResetLastWillRetainFlag_GivenMessageWithLastWillRetainFalse()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0010_0000;
        Assert.AreEqual(0b0000_0000, actual);
    }

    [TestMethod]
    public void SetLastWillQoSFlags0b00_GivenMessageWithLastWillQoSAtMostOnce()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0001_1000;
        Assert.AreEqual(0b0000_0000, actual);
    }

    [TestMethod]
    public void SetLastWillQoSFlags0b01_GivenMessageWithLastWillQoSAtLeastOnce()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(), willQoS: QoSLevel.QoS1).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0001_1000;
        Assert.AreEqual(0b0000_1000, actual);
    }

    [TestMethod]
    public void SetLastWillQoSFlags0b10_GivenMessageWithLastWillQoSExactlyOnce()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(), willQoS: QoSLevel.QoS2).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0001_1000;
        Assert.AreEqual(0b0001_0000, actual);
    }

    [TestMethod]
    public void NotSetLastWillPresentFlag_GivenMessageWithLastWillTopicNull()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(0b0000_0000, actual);
    }

    [TestMethod]
    public void NotSetLastWillPresentFlag_GivenMessageWithLastWillTopicEmpty()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(), willTopic: ReadOnlyMemory<byte>.Empty).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(0b0000_0000, actual);
    }

    [TestMethod]
    public void NotSetLastWillPresentFlag_GivenMessageWithLastWillMessageOnly()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(),
            willMessage: "last-will-packet"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(0b0000_0000, actual);
    }

    [TestMethod]
    public void SetLastWillPresentFlag_GivenMessageWithLastWillTopicNotEmpty()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(),
            willTopic: "last/will/topic"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(0b0000_0100, actual);
    }

    [TestMethod]
    public void EncodeZeroBytesMessage_GivenMessageWithLastWillTopicOnly()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(),
            willTopic: "last/will/topic"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        Assert.AreEqual(47, bytes.Length);
        Assert.AreEqual(0, bytes[45]);
        Assert.AreEqual(0, bytes[46]);
    }

    [TestMethod]
    public void SetUserNamePresentFlag_GivenMessageWithUserNameNotEmpty()
    {
        var writer = new ArrayBufferWriter<byte>(38);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(),
            userName: "TestUser"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(38, written);
        Assert.AreEqual(38, writer.WrittenCount);

        var actual = bytes[9] & 0b1000_0000;
        Assert.AreEqual(0b1000_0000, actual);
    }

    [TestMethod]
    public void ResetUserNamePresentFlag_GivenMessageWithUserNameNull()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b1000_0000;
        Assert.AreEqual(0b0000_0000, actual);
    }

    [TestMethod]
    public void SetPasswordPresentFlag_GivenMessageWithPasswordNotEmpty()
    {
        var writer = new ArrayBufferWriter<byte>(42);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray(),
            password: "TestPassword"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(42, written);
        Assert.AreEqual(42, writer.WrittenCount);

        var actual = bytes[9] & 0b0100_0000;
        Assert.AreEqual(0b0100_0000, actual);
    }

    [TestMethod]
    public void ResetPasswordPresentFlag_GivenMessageWithPasswordNull()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        var written = new Packets.V3.ConnectPacket("test-client-id"u8.ToArray(), 0x04, "MQTT"u8.ToArray()).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        var actual = bytes[9] & 0b0100_0000;
        Assert.AreEqual(0b0000_0000, actual);
    }
}