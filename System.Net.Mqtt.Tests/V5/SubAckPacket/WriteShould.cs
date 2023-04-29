using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.SubAckPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var bytes = new byte[8];
        new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(bytes, 6);

        const int expectedHeaderFlags = 0b1001_0000;
        Assert.AreEqual(expectedHeaderFlags, bytes[0]);

        const int expectedRemainingLength = 0x06;
        Assert.AreEqual(expectedRemainingLength, bytes[1]);
    }

    [TestMethod]
    public void EncodePacketId_GivenSampleMessage()
    {
        Span<byte> bytes = new byte[8];
        new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(bytes, 6);

        const byte expectedPacketId = 0x02;
        Assert.AreEqual(expectedPacketId, BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]));
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var bytes = new byte[8];
        new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(bytes, 6);

        Assert.AreEqual(1, bytes[5]);
        Assert.AreEqual(0, bytes[6]);
        Assert.AreEqual(2, bytes[7]);
    }

    [TestMethod]
    public void EncodeReasonStringBytes_GivenSampleMessage()
    {
        var bytes = new byte[21];
        new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }) { ReasonString = "any reason"u8.ToArray() }.Write(bytes, 19);

        Assert.AreEqual(13, bytes[4]);
        Assert.AreEqual(0x1F, bytes[5]);
        Assert.AreEqual(10, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(6)));
        Assert.IsTrue(bytes.AsSpan(8, 10).SequenceEqual("any reason"u8));
    }

    [TestMethod]
    public void EncodeUserPropertyBytes_GivenSampleMessage()
    {
        var bytes = new byte[40];
        new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 })
        {
            Properties = new List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)>()
            {
                ("prop1"u8.ToArray(), "value1"u8.ToArray()),
                ("prop2"u8.ToArray(), "value2"u8.ToArray())
            }
        }.Write(bytes, 38);

        Assert.AreEqual(32, bytes[4]);

        Assert.AreEqual(0x26, bytes[5]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(6)));
        Assert.IsTrue(bytes.AsSpan(8, 5).SequenceEqual("prop1"u8));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(13)));
        Assert.IsTrue(bytes.AsSpan(15, 6).SequenceEqual("value1"u8));

        Assert.AreEqual(0x26, bytes[21]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(22)));
        Assert.IsTrue(bytes.AsSpan(24, 5).SequenceEqual("prop2"u8));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(29)));
        Assert.IsTrue(bytes.AsSpan(31, 6).SequenceEqual("value2"u8));
    }
}