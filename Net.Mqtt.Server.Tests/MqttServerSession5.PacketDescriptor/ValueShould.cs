using Net.Mqtt.Packets.V5;
using MqttSession5 = Net.Mqtt.Server.Protocol.V5.MqttServerSession5;

namespace Net.Mqtt.Server.Tests.MqttServerSession5.PacketDescriptor;

[TestClass]
public class ValueShould
{
    [TestMethod]
    public void ReturnPacket_GivenPacketDescriptorContainingPacket()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        MqttSession5.PacketDescriptor descriptor = packet;

        var result = descriptor.Value;

        Assert.AreSame(packet, result);
    }

    [TestMethod]
    public void ReturnUInt32_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession5.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.Value;

        Assert.IsTrue(result is uint);
        Assert.AreEqual(raw32bit, result);
    }

    [TestMethod]
    public void ReturnUInt16_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession5.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.Value;

        Assert.IsTrue(result is ushort);
        Assert.AreEqual(raw16bit, result);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        MqttSession5.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.Value;

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        MqttSession5.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.Value;

        Assert.IsNull(result);
    }
}