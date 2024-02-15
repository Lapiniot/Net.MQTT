using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V5.SubAckPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        var _ = new Packets.V5.SubAckPacket(0, new byte[] { 0 });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionGivenResultParamEmpty()
    {
        var _ = new Packets.V5.SubAckPacket(1, default);
    }
}