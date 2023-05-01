using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.UnsubAckPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.UnsubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(writer, out var bytes);

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.AreEqual(0b1011_0000, bytes[0]);
        Assert.AreEqual(0x06, bytes[1]);
    }

    [TestMethod]
    public void EncodePacketId_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.UnsubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(writer, out var bytes);

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.AreEqual((byte)0x02, BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]));
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.UnsubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(writer, out var bytes);

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.AreEqual(1, bytes[5]);
        Assert.AreEqual(0, bytes[6]);
        Assert.AreEqual(2, bytes[7]);
    }

    [TestMethod]
    public void EncodeReasonStringBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(21);
        var written = new Packets.V5.UnsubAckPacket(0x02, new byte[] { 1, 0, 2 }) { ReasonString = "any reason"u8.ToArray() }.Write(writer, out var bytes);

        Assert.AreEqual(21, written);
        Assert.AreEqual(21, writer.WrittenCount);

        Assert.AreEqual(13, bytes[4]);
        Assert.AreEqual(0x1F, bytes[5]);
        Assert.AreEqual(10, BinaryPrimitives.ReadUInt16BigEndian(bytes[6..]));
        Assert.IsTrue(bytes.Slice(8, 10).SequenceEqual("any reason"u8));
    }

    [TestMethod]
    public void EncodeUserPropertyBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(40);
        var written = new Packets.V5.UnsubAckPacket(0x02, new byte[] { 1, 0, 2 })
        {
            Properties = new List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)>()
            {
                ("prop1"u8.ToArray(), "value1"u8.ToArray()),
                ("prop2"u8.ToArray(), "value2"u8.ToArray())
            }
        }.Write(writer, out var bytes);

        Assert.AreEqual(40, written);
        Assert.AreEqual(40, writer.WrittenCount);

        Assert.AreEqual(32, bytes[4]);

        Assert.AreEqual(0x26, bytes[5]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes[6..]));
        Assert.IsTrue(bytes.Slice(8, 5).SequenceEqual("prop1"u8));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes[13..]));
        Assert.IsTrue(bytes.Slice(15, 6).SequenceEqual("value1"u8));

        Assert.AreEqual(0x26, bytes[21]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes[22..]));
        Assert.IsTrue(bytes.Slice(24, 5).SequenceEqual("prop2"u8));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes[29..]));
        Assert.IsTrue(bytes.Slice(31, 6).SequenceEqual("value2"u8));
    }
}