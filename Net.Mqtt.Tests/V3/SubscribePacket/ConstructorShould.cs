using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V3.SubscribePacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        var _ = new Packets.V3.SubscribePacket(0, new (ReadOnlyMemory<byte>, byte)[] { ("topic1"u8.ToArray(), 0) });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ThrowArgumentNullExceptionGivenTopicsNull()
    {
        var _ = new Packets.V3.SubscribePacket(1, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicsEmpty()
    {
        var _ = new Packets.V3.SubscribePacket(1, []);
    }
}