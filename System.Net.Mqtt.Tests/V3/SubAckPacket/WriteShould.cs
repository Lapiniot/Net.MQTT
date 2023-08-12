using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V3.SubAckPacket;

[TestClass]
public class WriteShould
{
    private readonly Packets.V3.SubAckPacket samplePacket = new(0x02, [1, 0, 2]);

    [TestMethod]
    public void EncodeHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(7);
        var written = samplePacket.Write(writer, out var span);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        var actualHeaderFlags = span[0];
        Assert.AreEqual(0b1001_0000, actualHeaderFlags);

        var actualRemainingLength = span[1];
        Assert.AreEqual(0x05, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePacketId_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(7);
        var written = samplePacket.Write(writer, out var span);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(span[2..]);
        Assert.AreEqual((byte)0x0002, actualPacketId);
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(7);
        var written = samplePacket.Write(writer, out var span);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        Assert.AreEqual(1, span[4]);
        Assert.AreEqual(0, span[5]);
        Assert.AreEqual(2, span[6]);
    }
}