using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V5.PublishPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void NotThrowExceptionGivenTopicEmpty()
    {
        _ = new Packets.V5.PublishPacket(id: 0, qoSLevel: default,
            topic: Array.Empty<byte>(),
            payload: new byte[] { 0, 1, 2 });
    }

    [TestMethod]
    public void NotThrowExceptionGivenPayloadEmpty()
    {
        _ = new Packets.V5.PublishPacket(id: 0, qoSLevel: default,
            topic: "topic1"u8.ToArray(),
            payload: Array.Empty<byte>());
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