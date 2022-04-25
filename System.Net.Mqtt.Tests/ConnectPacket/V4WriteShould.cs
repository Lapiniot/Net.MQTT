﻿using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnectPacket;

[TestClass]
public class V4WriteShould
{
    private readonly Packets.ConnectPacket samplePacket =
        new(UTF8.GetBytes("TestClientId"), 0x04, UTF8.GetBytes("MQTT"), 120, true,
            UTF8.GetBytes("TestUser"), UTF8.GetBytes("TestPassword"),
            UTF8.GetBytes("TestWillTopic"), UTF8.GetBytes("TestWillMessage"));

    [TestMethod]
    public void SetHeaderBytes1680GivenSampleMessage()
    {
        Span<byte> bytes = new byte[82];
        samplePacket.Write(bytes, 80);

        const byte expectedPacketType = 0b0001_0000;
        var actualPacketType = bytes[0];
        Assert.AreEqual(expectedPacketType, actualPacketType);

        const int expectedRemainingLength = 80;
        var actualRemainingLength = bytes[1];
        Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
    }

    [TestMethod]
    public void SetProtocolInfoBytesGivenSampleMessage()
    {
        Span<byte> bytes = new byte[82];
        samplePacket.Write(bytes, 80);

        const int expectedProtocolNameLength = 4;
        var actualProtocolNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual(expectedProtocolNameLength, actualProtocolNameLength);

        const string expectedProtocolName = "MQTT";
        var actualProtocolName = UTF8.GetString(bytes.Slice(4, 4));
        Assert.AreEqual(expectedProtocolName, actualProtocolName);

        const int expectedProtocolVersion = 0x4;
        var actualProtocolVersion = bytes[8];
        Assert.AreEqual(expectedProtocolVersion, actualProtocolVersion);
    }

    [TestMethod]
    public void SetKeepAliveBytes0X0E10GivenMessageWithKeepAlive3600()
    {
        Span<byte> bytes = new byte[14];
        new Packets.ConnectPacket(null, 0x04, UTF8.GetBytes("MQTT"), 3600).Write(bytes, 12);

        Assert.AreEqual(0x0e, bytes[10]);
        Assert.AreEqual(0x10, bytes[11]);
    }

    [TestMethod]
    public void EncodeClientIdTestClientIdGivenSampleMessage()
    {
        Span<byte> bytes = new byte[82];
        samplePacket.Write(bytes, 80);

        const int expectedClientIdLength = 12;
        var actualClientIdLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[12..]);
        Assert.AreEqual(expectedClientIdLength, actualClientIdLength);

        const string expectedClientId = "TestClientId";
        var actualClientId = UTF8.GetString(bytes.Slice(14, expectedClientIdLength));
        Assert.AreEqual(expectedClientId, actualClientId);
    }

    [TestMethod]
    public void EncodeWillTopicTestWillTopicGivenSampleMessage()
    {
        Span<byte> bytes = new byte[82];
        samplePacket.Write(bytes, 80);

        const int expectedWillTopicLength = 13;
        var actualWillTopicLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[26..]);
        Assert.AreEqual(expectedWillTopicLength, actualWillTopicLength);

        const string expectedWillTopic = "TestWillTopic";
        var actualWillTopic = UTF8.GetString(bytes.Slice(28, expectedWillTopicLength));
        Assert.AreEqual(expectedWillTopic, actualWillTopic);
    }

    [TestMethod]
    public void EncodeWillMessageTestWillMessageGivenSampleMessage()
    {
        Span<byte> bytes = new byte[82];
        samplePacket.Write(bytes, 80);

        const int expectedWillMessageLength = 15;
        var actualWillMessageLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[41..]);
        Assert.AreEqual(expectedWillMessageLength, actualWillMessageLength);

        const string expectedWillMessage = "TestWillMessage";
        var actualWillMessage = UTF8.GetString(bytes.Slice(43, expectedWillMessageLength));
        Assert.AreEqual(expectedWillMessage, actualWillMessage);
    }

    [TestMethod]
    public void EncodeUserNameTestUserGivenSampleMessage()
    {
        Span<byte> bytes = new byte[82];
        samplePacket.Write(bytes, 80);

        const int expectedUserNameLength = 8;
        var actualUserNameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[58..]);
        Assert.AreEqual(expectedUserNameLength, actualUserNameLength);

        const string expectedUserName = "TestUser";
        var actualUserName = UTF8.GetString(bytes.Slice(60, expectedUserNameLength));
        Assert.AreEqual(expectedUserName, actualUserName);
    }

    [TestMethod]
    public void EncodePasswordTestPasswordGivenSampleMessage()
    {
        Span<byte> bytes = new byte[82];
        samplePacket.Write(bytes, 80);

        const int expectedPasswordLength = 12;
        var actualPasswordLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[68..]);
        Assert.AreEqual(expectedPasswordLength, actualPasswordLength);

        const string expectedPassword = "TestPassword";
        var actualPassword = UTF8.GetString(bytes.Slice(70, expectedPasswordLength));
        Assert.AreEqual(expectedPassword, actualPassword);
    }

    [TestMethod]
    public void SetCleanSessionFlagGivenMessageWithCleanSessionTrue()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT")).Write(bytes, 26);

        const int expected = 0b0000_0010;
        var actual = bytes[9] & 0b0000_0010;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ResetCleanSessionFlagGivenMessageWithCleanSessionFalse()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"), cleanSession: false).Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b0000_0010;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SetLastWillRetainFlagGivenMessageWithLastWillRetainTrue()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"), willRetain: true).Write(bytes, 28);

        const int expected = 0b0010_0000;
        var actual = bytes[9] & 0b0010_0000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ResetLastWillRetainFlagGivenMessageWithLastWillRetainFalse()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT")).Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b0010_0000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SetLastWillQoSFlags0b00GivenMessageWithLastWillQoSAtMostOnce()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT")).Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b0001_1000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SetLastWillQoSFlags0b01GivenMessageWithLastWillQoSAtLeastOnce()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"), willQoS: 1).Write(bytes, 26);

        const int expected = 0b0000_1000;
        var actual = bytes[9] & 0b0001_1000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SetLastWillQoSFlags0b10GivenMessageWithLastWillQoSExactlyOnce()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"), willQoS: 2).Write(bytes, 26);

        const int expected = 0b0001_0000;
        var actual = bytes[9] & 0b0001_1000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void NotSetLastWillPresentFlagGivenMessageWithLastWillTopicNull()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT")).Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void NotSetLastWillPresentFlagGivenMessageWithLastWillTopicEmpty()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04,
            UTF8.GetBytes("MQTT"), willTopic: ReadOnlyMemory<byte>.Empty)
            .Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void NotSetLastWillPresentFlagGivenMessageWithLastWillMessageOnly()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04,
            UTF8.GetBytes("MQTT"), willMessage: UTF8.GetBytes("last-will-packet"))
            .Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SetLastWillPresentFlagGivenMessageWithLastWillTopicNotEmpty()
    {
        Span<byte> bytes = new byte[47];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04,
            UTF8.GetBytes("MQTT"), willTopic: UTF8.GetBytes("last/will/topic"))
            .Write(bytes, 45);

        const int expected = 0b0000_0100;
        var actual = bytes[9] & 0b0000_0100;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void EncodeZeroBytesMessageGivenMessageWithLastWillTopicOnly()
    {
        Span<byte> bytes = new byte[47];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04,
            UTF8.GetBytes("MQTT"), willTopic: UTF8.GetBytes("last/will/topic"))
            .Write(bytes, 45);

        Assert.AreEqual(47, bytes.Length);
        Assert.AreEqual(0, bytes[45]);
        Assert.AreEqual(0, bytes[46]);
    }

    [TestMethod]
    public void SetUserNamePresentFlagGivenMessageWithUserNameNotEmpty()
    {
        Span<byte> bytes = new byte[38];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"), userName: UTF8.GetBytes("TestUser")).Write(bytes, 36);

        const int expected = 0b1000_0000;
        var actual = bytes[9] & 0b1000_0000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ResetUserNamePresentFlagGivenMessageWithUserNameNull()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT")).Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b1000_0000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SetPasswordPresentFlagGivenMessageWithPasswordNotEmpty()
    {
        Span<byte> bytes = new byte[42];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT"), password: UTF8.GetBytes("TestPassword")).Write(bytes, 40);

        const int expected = 0b0100_0000;
        var actual = bytes[9] & 0b0100_0000;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ResetPasswordPresentFlagGivenMessageWithPasswordNull()
    {
        Span<byte> bytes = new byte[28];
        new Packets.ConnectPacket(UTF8.GetBytes("test-client-id"), 0x04, UTF8.GetBytes("MQTT")).Write(bytes, 26);

        const int expected = 0b0000_0000;
        var actual = bytes[9] & 0b0100_0000;
        Assert.AreEqual(expected, actual);
    }
}