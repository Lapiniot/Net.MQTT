using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnAckPacket;

[TestClass]
public record class WriteShould
{
    private readonly Packets.ConnAckPacket samplePacket = new(0x02, true);

    [TestMethod]
    public void ThrowIndexOutOfRangeException_GivenUnsufficientSpanSize()
    {
        var bytes = new byte[2];
        Assert.ThrowsException<IndexOutOfRangeException>(() => samplePacket.Write(bytes, 2));
    }

    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var bytes = new byte[4];
        samplePacket.Write(bytes, 2);

        const int expectedHeaderFlags = 0b100000;
        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

        const int expectedRemainingLength = 0x02;
        var actualRemainingLength = bytes[1];
        Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var bytes = new byte[4];
        new Packets.ConnAckPacket(0x02, true).Write(bytes, 2);

        Assert.AreEqual(0x1, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);

        new Packets.ConnAckPacket(0x02, false).Write(bytes, 2);

        Assert.AreEqual(0x0, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);
    }
}