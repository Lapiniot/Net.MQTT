using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.ConnAckPacket;

[TestClass]
public class GetSizeShould
{
    [TestMethod]
    public void Return4AndRemainingLength2()
    {
        var packet = new Packets.V5.ConnAckPacket(0x02, true);

        var actual = packet.GetSize(out var remainingLength);
        Assert.AreEqual(5, actual);
        Assert.AreEqual(2, remainingLength);
    }
}