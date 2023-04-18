﻿using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V3.SubAckPacket;

[TestClass]
public class WriteShould
{
    private readonly Packets.V3.SubAckPacket samplePacket = new(0x02, new byte[] { 1, 0, 2 });

    [TestMethod]
    public void SetHeaderBytes0X900X05GivenSampleMessage()
    {
        var bytes = new byte[7];
        samplePacket.Write(bytes, 5);

        const int expectedHeaderFlags = 0b1001_0000;
        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

        const int expectedRemainingLength = 0x05;
        var actualRemainingLength = bytes[1];
        Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePacketId0X0002GivenSampleMessage()
    {
        Span<byte> bytes = new byte[7];
        samplePacket.Write(bytes, 5);

        const byte expectedPacketId = 0x0002;
        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual(expectedPacketId, actualPacketId);
    }

    [TestMethod]
    public void EncodeResultBytesGivenSampleMessage()
    {
        var bytes = new byte[7];
        samplePacket.Write(bytes, 5);

        Assert.AreEqual(1, bytes[4]);
        Assert.AreEqual(0, bytes[5]);
        Assert.AreEqual(2, bytes[6]);
    }
}