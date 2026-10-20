using Net.Mqtt.Packets.V3;
using MqttSession3 = Net.Mqtt.Server.Protocol.V3.MqttServerSession3;

namespace Net.Mqtt.Server.Tests.MqttServerSession3.PacketDescriptor;

[TestClass]
public class HasValueShould
{
    [TestMethod]
    public void ReturnTrue_GivenPacketDescriptorContainingPacket()
    {
        var packet = new SubAckPacket(0x2A, [0x00, 0x01]);
        MqttSession3.PacketDescriptor descriptor = packet;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnTrue_GivenPublishBlockValue()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        MqttSession3.PacketDescriptor descriptor = (topic, ReadOnlyMemory<byte>.Empty, 0);

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenDefaultValue()
    {
        MqttSession3.PacketDescriptor descriptor = default;

        var actual = descriptor.HasValue;

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.HasValue;

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.HasValue;

        Assert.IsFalse(result);
    }
}