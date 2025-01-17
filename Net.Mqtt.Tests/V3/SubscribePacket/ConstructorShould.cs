namespace Net.Mqtt.Tests.V3.SubscribePacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            _ = new Packets.V3.SubscribePacket(0, [("topic1"u8.ToArray(), 0)]));
    }

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenTopicsNull()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            _ = new Packets.V3.SubscribePacket(1, null));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicsEmpty()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            _ = new Packets.V3.SubscribePacket(1, []));
    }
}