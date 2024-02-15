using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V5.PublishPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicEmpty()
    {
        _ = new Packets.V5.PublishPacket(0, default, default);
    }

    [TestMethod]
    public void NotThrowArgumentExceptionGivenQoS0AndNoPacketId()
    {
        var _ = new Packets.V5.PublishPacket(0, 0, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS0AndPacketIdNotZero()
    {
        var _ = new Packets.V5.PublishPacket(100, QoSLevel.QoS0, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS1AndNoPacketId()
    {
        var _ = new Packets.V5.PublishPacket(0, QoSLevel.QoS1, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS2AndNoPacketId()
    {
        var _ = new Packets.V5.PublishPacket(0, QoSLevel.QoS2, "/"u8.ToArray());
    }
}