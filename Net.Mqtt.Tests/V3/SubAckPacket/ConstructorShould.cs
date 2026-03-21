namespace Net.Mqtt.Tests.V3.SubAckPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Packets.V3.SubAckPacket(0, [0]));
    }

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenResultParamNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Packets.V3.SubAckPacket(1, null));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenResultParamEmpty()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Packets.V3.SubAckPacket(1, []));
    }
}