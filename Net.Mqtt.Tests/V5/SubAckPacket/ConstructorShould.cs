namespace Net.Mqtt.Tests.V5.SubAckPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            _ = new Packets.V5.SubAckPacket(0, new byte[] { 0 }));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenResultParamEmpty()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            _ = new Packets.V5.SubAckPacket(1, default));
    }
}