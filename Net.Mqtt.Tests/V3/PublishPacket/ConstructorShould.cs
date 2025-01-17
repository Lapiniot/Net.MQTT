namespace Net.Mqtt.Tests.V3.PublishPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicEmpty()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            _ = new Packets.V3.PublishPacket(0, default, topic: Array.Empty<byte>(), payload: new byte[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void NotThrowExceptionGivenPayloadEmpty()
    {
        _ = new Packets.V3.PublishPacket(id: 0, qoSLevel: default,
            topic: "topic1"u8.ToArray(),
            payload: Array.Empty<byte>());
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenQoS0AndPacketIdNotZero()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            _ = new Packets.V3.PublishPacket(100, QoSLevel.QoS0, "/"u8.ToArray()));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenQoS1AndNoPacketId()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            _ = new Packets.V3.PublishPacket(0, QoSLevel.QoS1, "/"u8.ToArray()));
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenQoS2AndNoPacketId()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            _ = new Packets.V3.PublishPacket(0, QoSLevel.QoS2, "/"u8.ToArray()));
    }

    [TestMethod]
    public void NotThrowArgumentExceptionGivenQoS0AndNoPacketId()
    {
        _ = new Packets.V3.PublishPacket(0, 0, "/"u8.ToArray());
    }
}