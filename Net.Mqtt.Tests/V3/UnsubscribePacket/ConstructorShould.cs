namespace Net.Mqtt.Tests.V3.UnsubscribePacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new Packets.V3.UnsubscribePacket(0, ["topic1"u8.ToArray()]));
    }

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenTopicsNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new Packets.V3.UnsubscribePacket(1, null));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicsEmpty()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new Packets.V3.UnsubscribePacket(1, []));
    }
}