﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Packets.V5.ConnectPacket;

namespace System.Net.Mqtt.Tests.V5.ConnectPacket;

[TestClass]
public class TryReadShould
{
    [TestMethod]
    public void ReturnTrue_PacketNotNull_GivenValidSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0x10, 0xf8, 0x01, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xf6, 0x00, 0x3c, 0x56, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x21, 0x04,
            0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00, 0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d,
            0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c,
            0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x11, 0x75,
            0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x32, 0x00, 0x0e, 0x6d, 0x71, 0x74,
            0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x44, 0x18, 0x00, 0x00, 0x00, 0x78, 0x01, 0x01, 0x02, 0x00,
            0x00, 0x01, 0x2c, 0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e, 0x08, 0x00, 0x15, 0x2f, 0x77,
            0x69, 0x6c, 0x6c, 0x2d, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x09, 0x00,
            0x10, 0x63, 0x6f, 0x72, 0x72, 0x65, 0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x00, 0x11, 0x2f, 0x6c,
            0x61, 0x73, 0x74, 0x2d, 0x77, 0x69, 0x6c, 0x6c, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x00, 0x17, 0x4c, 0x61, 0x73, 0x74,
            0x2d, 0x57, 0x69, 0x6c, 0x6c, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x74, 0x65, 0x73, 0x74, 0x61, 0x6d, 0x65, 0x6e, 0x74, 0x00, 0x09,
            0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72, 0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x70, 0x61, 0x73, 0x73 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.IsTrue(packet.ProtocolName.Span.SequenceEqual("MQTT"u8));
        Assert.AreEqual(0x05, packet.ProtocolLevel);
        Assert.AreEqual(2, packet.WillQoS);
        Assert.IsTrue(packet.WillRetain);
        Assert.IsTrue(packet.CleanStart);
        Assert.AreEqual(60, packet.KeepAlive);
        Assert.IsTrue(packet.ClientId.Span.SequenceEqual("mqttx_adfb8557"u8));
        Assert.IsTrue(packet.WillTopic.Span.SequenceEqual("/last-will/topic1"u8));
        Assert.IsTrue(packet.WillMessage.Span.SequenceEqual("Last-Will and testament"u8));
        Assert.IsTrue(packet.UserName.Span.SequenceEqual("mqtt-user"u8));
        Assert.IsTrue(packet.Password.Span.SequenceEqual("mqtt-pass"u8));
    }

    [TestMethod]
    public void ReturnTrue_PacketNotNull_GivenValidFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x10, 0xf8, 0x01, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xf6, 0x00, 0x3c, 0x56, 0x11,
                0x00, 0x00, 0x01, 0x2c, 0x21, 0x04, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00,
                0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f,
                0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d,
                0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d,
                0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72,
                0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x32, 0x00, 0x0e, 0x6d, 0x71, 0x74 },
            new byte[] {
                0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x44, 0x18, 0x00, 0x00,
                0x00, 0x78, 0x01, 0x01, 0x02, 0x00, 0x00, 0x01, 0x2c, 0x03, 0x00, 0x0a, 0x74, 0x65, 0x78,
                0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e, 0x08, 0x00, 0x15, 0x2f, 0x77, 0x69, 0x6c, 0x6c,
                0x2d, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63,
                0x31, 0x09, 0x00, 0x10, 0x63, 0x6f, 0x72, 0x72, 0x65, 0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e,
                0x2d, 0x64, 0x61, 0x74, 0x61, 0x00, 0x11, 0x2f, 0x6c, 0x61, 0x73, 0x74, 0x2d, 0x77, 0x69,
                0x6c, 0x6c, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x00, 0x17, 0x4c, 0x61, 0x73, 0x74 },
            new byte[] {
                0x2d, 0x57, 0x69, 0x6c, 0x6c, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x74, 0x65, 0x73, 0x74, 0x61,
                0x6d, 0x65, 0x6e, 0x74, 0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72,
                0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x70, 0x61, 0x73, 0x73 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.IsTrue(packet.ProtocolName.Span.SequenceEqual("MQTT"u8));
        Assert.AreEqual(0x05, packet.ProtocolLevel);
        Assert.AreEqual(2, packet.WillQoS);
        Assert.IsTrue(packet.WillRetain);
        Assert.IsTrue(packet.CleanStart);
        Assert.AreEqual(60, packet.KeepAlive);
        Assert.IsTrue(packet.ClientId.Span.SequenceEqual("mqttx_adfb8557"u8));
        Assert.IsTrue(packet.WillTopic.Span.SequenceEqual("/last-will/topic1"u8));
        Assert.IsTrue(packet.WillMessage.Span.SequenceEqual("Last-Will and testament"u8));
        Assert.IsTrue(packet.UserName.Span.SequenceEqual("mqtt-user"u8));
        Assert.IsTrue(packet.Password.Span.SequenceEqual("mqtt-pass"u8));
    }

    [TestMethod]
    public void ReturnTrue_PacketNotNull_WillTopicEmpty_GivenSampleWithoutWillMessage()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0x10, 0x87, 0x01, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xc2, 0x00, 0x3c, 0x56, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x21,
            0x04, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00, 0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65,
            0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d,
            0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d,
            0x32, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x32,
            0x00, 0x0e, 0x6d, 0x71, 0x74, 0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x00, 0x09, 0x6d, 0x71,
            0x74, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72, 0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x70, 0x61, 0x73, 0x73 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.IsTrue(packet.WillTopic.IsEmpty);
        Assert.AreEqual(0, packet.WillMessage.Length);
    }

    [TestMethod]
    public void ReturnTrue_PacketNotNull_ClientIdEmpty_GivenSampleWithoutClientId()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0x10, 0x79, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xc2, 0x00, 0x3c, 0x56, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x21,
            0x04, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00, 0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73,
            0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f,
            0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72,
            0x6f, 0x70, 0x2d, 0x32, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c,
            0x75, 0x65, 0x2d, 0x32, 0x00, 0x00, 0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72, 0x00, 0x09,
            0x6d, 0x71, 0x74, 0x74, 0x2d, 0x70, 0x61, 0x73, 0x73 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.IsTrue(packet.ClientId.IsEmpty);
    }

    [TestMethod]
    public void ReturnTrue_PacketNotNull_UserNameEmpty_GivenSampleWithoutUserName()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0x10, 0x21, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0x82, 0x00, 0x3c, 0x04, 0x19, 0x00, 0x17, 0x00, 0x00,
            0x0e, 0x6d, 0x71, 0x74, 0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x00, 0x00 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.IsTrue(packet.UserName.IsEmpty);
    }

    [TestMethod]
    public void ReturnTrue_PacketNotNull_PasswordEmpty_GivenSampleWithoutPassword()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0x10, 0x2a, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0x82, 0x00, 0x3c, 0x04, 0x19, 0x00,
            0x17, 0x00, 0x00, 0x0e, 0x6d, 0x71, 0x74, 0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38,
            0x35, 0x35, 0x37, 0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.IsTrue(packet.Password.IsEmpty);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenInvalidTypeSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0x12, 0xf8, 0x01, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xf6, 0x00, 0x3c, 0x56, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x21, 0x04,
            0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00, 0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d,
            0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c,
            0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x11, 0x75,
            0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x32, 0x00, 0x0e, 0x6d, 0x71, 0x74,
            0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x44, 0x18, 0x00, 0x00, 0x00, 0x78, 0x01, 0x01, 0x02, 0x00,
            0x00, 0x01, 0x2c, 0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e, 0x08, 0x00, 0x15, 0x2f, 0x77,
            0x69, 0x6c, 0x6c, 0x2d, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x09, 0x00,
            0x10, 0x63, 0x6f, 0x72, 0x72, 0x65, 0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x00, 0x11, 0x2f, 0x6c,
            0x61, 0x73, 0x74, 0x2d, 0x77, 0x69, 0x6c, 0x6c, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x00, 0x17, 0x4c, 0x61, 0x73, 0x74,
            0x2d, 0x57, 0x69, 0x6c, 0x6c, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x74, 0x65, 0x73, 0x74, 0x61, 0x6d, 0x65, 0x6e, 0x74, 0x00, 0x09,
            0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72, 0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x70, 0x61, 0x73, 0x73 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenInvalidTypeFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x12, 0xf8, 0x01, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xf6, 0x00, 0x3c, 0x56, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x21, 0x04,
                0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00, 0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d,
                0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c,
                0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x11, 0x75,
                0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x32, 0x00, 0x0e, 0x6d, 0x71, 0x74, },
            new byte[] {
                0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x44, 0x18, 0x00, 0x00, 0x00, 0x78, 0x01, 0x01, 0x02, 0x00,
                0x00, 0x01, 0x2c, 0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e, 0x08, 0x00, 0x15, 0x2f, 0x77,
                0x69, 0x6c, 0x6c, 0x2d, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x09, 0x00,
                0x10, 0x63, 0x6f, 0x72, 0x72, 0x65, 0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x00, 0x11, 0x2f, 0x6c },
            new byte[] {
                0x61, 0x73, 0x74, 0x2d, 0x77, 0x69, 0x6c, 0x6c, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x00, 0x17, 0x4c, 0x61, 0x73, 0x74,
                0x2d, 0x57, 0x69, 0x6c, 0x6c, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x74, 0x65, 0x73, 0x74, 0x61, 0x6d, 0x65, 0x6e, 0x74, 0x00, 0x09,
                0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72, 0x00, 0x09, 0x6d, 0x71, 0x74, 0x74, 0x2d, 0x70, 0x61, 0x73, 0x73 });

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0x12, 0xf8, 0x01, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xf6, 0x00, 0x3c, 0x56, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x21, 0x04,
            0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00, 0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d,
            0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c,
            0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x11, 0x75,
            0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x32, 0x00, 0x0e, 0x6d, 0x71, 0x74,
            0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x44, 0x18, 0x00, 0x00, 0x00, 0x78, 0x01, 0x01, 0x02, 0x00,
            0x00, 0x01, 0x2c, 0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e, 0x08, 0x00, 0x15, 0x2f, 0x77,
            0x69, 0x6c, 0x6c, 0x2d, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x09, 0x00,
            0x10, 0x63, 0x6f, 0x72, 0x72, 0x65, 0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x00, 0x11, 0x2f, 0x6c,
            0x61, 0x73, 0x74, 0x2d, 0x77, 0x69, 0x6c, 0x6c, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x00, 0x17, 0x4c, 0x61, 0x73, 0x74,
            0x2d, 0x57, 0x69, 0x6c, 0x6c, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x74, 0x65, 0x73, 0x74, 0x61, 0x6d, 0x65, 0x6e, 0x74, 0x00, 0x09,
            0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73, 0x65});

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x12, 0xf8, 0x01, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x05, 0xf6, 0x00, 0x3c, 0x56, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x21, 0x04,
                0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x22, 0x02, 0x00, 0x19, 0x01, 0x17, 0x01, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d,
                0x70, 0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x11, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c,
                0x75, 0x65, 0x2d, 0x31, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x11, 0x75,
                0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x2d, 0x32, 0x00, 0x0e, 0x6d, 0x71, 0x74, },
            new byte[] {
                0x74, 0x78, 0x5f, 0x61, 0x64, 0x66, 0x62, 0x38, 0x35, 0x35, 0x37, 0x44, 0x18, 0x00, 0x00, 0x00, 0x78, 0x01, 0x01, 0x02, 0x00,
                0x00, 0x01, 0x2c, 0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e, 0x08, 0x00, 0x15, 0x2f, 0x77,
                0x69, 0x6c, 0x6c, 0x2d, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x09, 0x00,
                0x10, 0x63, 0x6f, 0x72, 0x72, 0x65, 0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x00, 0x11, 0x2f, 0x6c },
            new byte[] {
                0x61, 0x73, 0x74, 0x2d, 0x77, 0x69, 0x6c, 0x6c, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x00, 0x17, 0x4c, 0x61, 0x73, 0x74,
                0x2d, 0x57, 0x69, 0x6c, 0x6c, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x74, 0x65, 0x73, 0x74, 0x61, 0x6d, 0x65, 0x6e, 0x74, 0x00, 0x09,
                0x6d, 0x71, 0x74, 0x74, 0x2d, 0x75, 0x73});

        var actual = TryRead(in sequence, out var packet, out _);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }
}