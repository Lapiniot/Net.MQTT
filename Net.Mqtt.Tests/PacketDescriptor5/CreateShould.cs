using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Tests.PacketDescriptor5;

[TestClass]
public class CreateShould
{
    [TestMethod]
    public void Store_PublishPacket_GivenValidPacketInstance()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        var descriptor = Mqtt.PacketDescriptor5.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is PublishPacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_ArbitraryPacket_GivenValidPacketInstance()
    {
        var packet = new PubAckPacket(7);
        var descriptor = Mqtt.PacketDescriptor5.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is PubAckPacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    [Description("Verify that valid raw 4-bytes sized packet data is stored correctly as a UInt32 value. " +
        "Value bits 28-31 encoding non-zero packet type, is considered a valid packet.")]
    public void Store_UInt32_GivenValidValue_WithPacketHeader()
    {
        const uint raw32bit = PacketFlags.UnsubAckPacketMask | 0x2A;
        var descriptor = Mqtt.PacketDescriptor5.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is uint);
        Assert.AreEqual(raw32bit, descriptor);
    }

    [TestMethod]
    [Description("Verify that valid raw 2-bytes sized packet data is stored correctly as a UInt16 value. " +
        "Value bits 12-15 encoding non-zero packet type, is considered a valid packet.")]
    public void Store_UInt16_GivenValidValue_WithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        var descriptor = Mqtt.PacketDescriptor5.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is ushort);
        Assert.AreEqual(raw16bit, descriptor);
    }

    [TestMethod]
    [Description("Verify that invalid raw 4-bytes sized packet data is ignored, so resulting value evaluates to null. " +
        "Bits 28-31 should encode the packet type, otherwise the value is considered invalid.")]
    public void Store_Null_GivenInvalid32bitValue()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        var descriptor = Mqtt.PacketDescriptor5.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is null);
    }

    [TestMethod]
    [Description("Verify that invalid raw 2-bytes sized packet data is ignored, so resulting value evaluates to null. " +
        "Bits 12-15 should encode the packet type, otherwise the value is considered invalid.")]
    public void Store_Null_GivenInvalid16bitValue()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        var descriptor = Mqtt.PacketDescriptor5.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is null);
    }
}