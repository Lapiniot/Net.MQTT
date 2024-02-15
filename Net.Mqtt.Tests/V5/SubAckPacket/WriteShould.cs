using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V5.SubAckPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.AreEqual(0b1001_0000, bytes[0]);
        Assert.AreEqual(0x06, bytes[1]);
    }

    [TestMethod]
    public void EncodePacketId_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.AreEqual((byte)0x02, BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]));
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

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
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }) { ReasonString = "any reason"u8.ToArray() }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(21, written);
        Assert.AreEqual(21, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x90, 0x13, 0x00, 0x02, 0x0d, 0x1f, 0x00, 0x0a, 0x61, 0x6e, 0x79,
            0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x01, 0x00, 0x02 }));
    }

    [TestMethod]
    public void OmitReasonString_IfSizeExceedsMaxAllowedBytes()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }) { ReasonString = "any reason"u8.ToArray() }.Write(writer, 16);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x90, 0x06, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02 }));
    }

    [TestMethod]
    public void EncodeUserPropertyBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(40);
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 })
        {
            UserProperties =
            [
                ("prop1"u8.ToArray(), "value1"u8.ToArray()),
                ("prop2"u8.ToArray(), "value2"u8.ToArray())
            ]
        }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(40, written);
        Assert.AreEqual(40, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x90, 0x26, 0x00, 0x02, 0x20, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00,
            0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70,
            0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x01, 0x00, 0x02 }));
    }

    [TestMethod]
    public void OmitUserProperties_IfSizeExceedsMaxAllowedBytes()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 })
        {
            UserProperties =
            [
                ("prop1"u8.ToArray(), "value1"u8.ToArray()),
                ("prop2"u8.ToArray(), "value2"u8.ToArray())
            ]
        }.Write(writer, 20);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x90, 0x06, 0x00, 0x02, 0x00, 0x01, 0x00, 0x02 }));
    }

    [TestMethod]
    public void WriteZeroBytes_IfSizeExceedsMaxAllowedBytes_AndNoPropsToOmit()
    {
        var writer = new ArrayBufferWriter<byte>(8);
        var written = new Packets.V5.SubAckPacket(0x02, new byte[] { 1, 0, 2 }).Write(writer, 4);

        Assert.AreEqual(0, written);
        Assert.AreEqual(0, writer.WrittenCount);
    }
}