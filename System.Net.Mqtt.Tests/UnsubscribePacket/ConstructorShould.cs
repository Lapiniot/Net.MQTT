using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.UnsubscribePacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionGivenPacketId0()
    {
        var _ = new Packets.V3.UnsubscribePacket(0, new ReadOnlyMemory<byte>[] { "topic1"u8.ToArray() });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ThrowArgumentNullExceptionGivenTopicsNull()
    {
        var _ = new Packets.V3.UnsubscribePacket(1, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenTopicsEmpty()
    {
        var _ = new Packets.V3.UnsubscribePacket(1, Array.Empty<ReadOnlyMemory<byte>>());
    }
}