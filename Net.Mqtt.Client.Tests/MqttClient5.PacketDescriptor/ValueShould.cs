using Net.Mqtt.Packets.V5;
using Client5 = Net.Mqtt.Client.MqttClient5;

namespace Net.Mqtt.Client.Tests.MqttClient5.PacketDescriptor;

[TestClass]
public class ValueShould
{
    [TestMethod]
    public void ReturnPacket_GivenPacketDescriptorContainingPacket()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        Client5.PacketDescriptor descriptor = packet;

        var actual = descriptor.Value;

        Assert.AreSame(packet, actual);
    }

    [TestMethod]
    public void ReturnUInt32_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client5.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.Value;

        Assert.IsTrue(actual is uint);
        Assert.AreEqual(raw32bit, actual);
    }

    [TestMethod]
    public void ReturnUInt16_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client5.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.Value;

        Assert.IsTrue(actual is ushort);
        Assert.AreEqual(raw16bit, actual);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0x0000_0001U;
        Client5.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.Value;

        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        Client5.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.Value;

        Assert.IsNull(actual);
    }
}