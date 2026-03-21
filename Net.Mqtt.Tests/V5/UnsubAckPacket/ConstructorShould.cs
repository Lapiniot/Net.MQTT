namespace Net.Mqtt.Tests.V5.UnsubAckPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Packets.V5.UnsubAckPacket(0, [0]));
    }

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenResultParamNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Packets.V5.UnsubAckPacket(1, null));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenResultParamEmpty()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Packets.V5.UnsubAckPacket(1, []));
    }
}