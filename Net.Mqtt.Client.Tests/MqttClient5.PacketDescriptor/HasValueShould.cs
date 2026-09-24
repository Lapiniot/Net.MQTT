using Net.Mqtt.Packets.V5;
using Client5 = Net.Mqtt.Client.MqttClient5;

namespace Net.Mqtt.Client.Tests.MqttClient5.PacketDescriptor;

[TestClass]
public class HasValueShould
{
    [TestMethod]
    public void ReturnTrue_GivenPacketDescriptorContainingPacket()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        Client5.PacketDescriptor descriptor = packet;

        var actual = descriptor.HasValue;

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client5.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.HasValue;

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client5.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.HasValue;

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0x0000_0001U;
        Client5.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.HasValue;

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        Client5.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.HasValue;

        Assert.IsFalse(actual);
    }
}