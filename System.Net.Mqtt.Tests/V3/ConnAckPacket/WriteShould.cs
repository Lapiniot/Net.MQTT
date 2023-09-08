using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V3.ConnAckPacket;

[TestClass]
public record class WriteShould
{
    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(4);

        var written = new Packets.V3.ConnAckPacket(0x02, true).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(4, written);
        Assert.AreEqual(4, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(0b100000, actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(0x02, actualRemainingLength);
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(4);

        var written = new Packets.V3.ConnAckPacket(0x02, true).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(4, written);
        Assert.AreEqual(4, writer.WrittenCount);

        Assert.AreEqual(0x1, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);

        writer.Clear();

        written = new Packets.V3.ConnAckPacket(0x02, false).Write(writer);
        bytes = writer.WrittenSpan;

        Assert.AreEqual(4, written);
        Assert.AreEqual(4, writer.WrittenCount);

        Assert.AreEqual(0x0, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);
    }
}