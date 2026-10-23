using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Tests.PacketDescriptor3;

[TestClass]
public class HasValueShould
{
    [TestMethod]
    public void ReturnTrue_GivenPacketDescriptorContainingPacket()
    {
        var packet = new SubAckPacket(0x2A, [0x00, 0x01]);
        Mqtt.PacketDescriptor3 descriptor = packet;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnTrue_GivenPublishBlockValue()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        Mqtt.PacketDescriptor3 descriptor = (topic, ReadOnlyMemory<byte>.Empty, 0);

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenDefaultValue()
    {
        Mqtt.PacketDescriptor3 descriptor = default;

        var actual = descriptor.HasValue;

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Mqtt.PacketDescriptor3 descriptor = raw32bit;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Mqtt.PacketDescriptor3 descriptor = raw16bit;

        var result = descriptor.HasValue;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        Mqtt.PacketDescriptor3 descriptor = raw32bit;

        var result = descriptor.HasValue;

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        Mqtt.PacketDescriptor3 descriptor = raw16bit;

        var result = descriptor.HasValue;

        Assert.IsFalse(result);
    }
}