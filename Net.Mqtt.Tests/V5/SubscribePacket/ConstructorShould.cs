namespace Net.Mqtt.Tests.V5.SubscribePacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new Packets.V5.SubscribePacket(0, [("topic1"u8.ToArray(), 0)]));
    }

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenTopicsNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new Packets.V5.SubscribePacket(1, null));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicsEmpty()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new Packets.V5.SubscribePacket(1, []));
    }
}