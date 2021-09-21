using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.MqttPacketWithId;

[TestClass]
public class GetSizeShould
{
    [TestMethod]
    public void Return4AndRemainingLength2()
    {
        var packet = new UnsubAckPacket(1);

        var size = packet.GetSize(out var remainingLength);

        Assert.AreEqual(4, size);
        Assert.AreEqual(2, remainingLength);
    }
}