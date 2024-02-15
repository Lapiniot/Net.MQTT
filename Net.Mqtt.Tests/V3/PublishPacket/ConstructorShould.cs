using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V3.PublishPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicEmpty()
    {
        _ = new Packets.V3.PublishPacket(0, default, default);
    }

    [TestMethod]
    public void NotThrowArgumentExceptionGivenQoS0AndNoPacketId()
    {
        _ = new Packets.V3.PublishPacket(0, 0, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS0AndPacketIdNotZero()
    {
        _ = new Packets.V3.PublishPacket(100, QoSLevel.QoS0, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS1AndNoPacketId()
    {
        _ = new Packets.V3.PublishPacket(0, QoSLevel.QoS1, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS2AndNoPacketId()
    {
        _ = new Packets.V3.PublishPacket(0, QoSLevel.QoS2, "/"u8.ToArray());
    }
}