using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Tests.PacketDescriptor5;

[TestClass]
public class HasValueShould
{
    [TestMethod]
    public void ReturnTrue_GivenPacketDescriptorContainingPacket()
    {
        var packet = new PubAckPacket(0x2A);
        Mqtt.PacketDescriptor5 descriptor = packet;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Mqtt.PacketDescriptor5 descriptor = raw32bit;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Mqtt.PacketDescriptor5 descriptor = raw16bit;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        Mqtt.PacketDescriptor5 descriptor = raw32bit;

        var result = descriptor.HasValue;

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        Mqtt.PacketDescriptor5 descriptor = raw16bit;

        var result = descriptor.HasValue;

        Assert.IsFalse(result);
    }
}