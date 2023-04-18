using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.ConnAckPacket;

[TestClass]
public record class WriteShould
{
    [TestMethod]
    public void ThrowIndexOutOfRangeException_GivenUnsufficientSpanSize()
    {
        var bytes = new byte[2];
        Assert.ThrowsException<IndexOutOfRangeException>(() => new Packets.V5.ConnAckPacket(0x02, true).Write(bytes, 2));
    }

    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var bytes = new byte[5];
        new Packets.V5.ConnAckPacket(0x02, true).Write(bytes, 3);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(0b100000, actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(0x03, actualRemainingLength);

        var actualPropsLength = bytes[4];
        Assert.AreEqual(0x00, actualPropsLength);
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var bytes = new byte[5];
        new Packets.V5.ConnAckPacket(0x02, true).Write(bytes, 3);

        Assert.AreEqual(0x1, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);

        new Packets.V5.ConnAckPacket(0x02, false).Write(bytes, 3);

        Assert.AreEqual(0x0, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);
    }
}