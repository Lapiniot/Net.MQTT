using Net.Mqtt.Packets.V3;

#nullable enable

namespace Net.Mqtt.Tests.PacketDescriptor3;

[TestClass]
public class ValueShould
{
    [TestMethod]
    public void ReturnPublishBlock_GivenTopicValue()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        ReadOnlyMemory<byte> payload = "testpayload"u8.ToArray();
        Mqtt.PacketDescriptor3 descriptor = (topic, payload, 42u);

        var result = descriptor.Value;

        Assert.IsNotNull(result);
        var (actualTopic, actualPayload, actualFlags) = (PublishBlock)result;
        Assert.AreSequenceEqual(topic, actualTopic);
        Assert.AreSequenceEqual(payload, actualPayload);
        Assert.AreEqual(42u, actualFlags);
    }

    [TestMethod]
    public void ReturnPacket_GivenPacketDescriptorContainingPacket()
    {
        var packet = new SubAckPacket(42, [0x00, 0x01]);
        Mqtt.PacketDescriptor3 descriptor = packet;

        var result = descriptor.Value;

        Assert.AreSame(packet, result);
    }

    [TestMethod]
    public void ReturnUInt32_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Mqtt.PacketDescriptor3 descriptor = raw32bit;

        var result = descriptor.Value;

        Assert.IsTrue(result is uint);
        Assert.AreEqual(raw32bit, result);
    }

    [TestMethod]
    public void ReturnUInt16_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Mqtt.PacketDescriptor3 descriptor = raw16bit;

        var result = descriptor.Value;

        Assert.IsTrue(result is ushort);
        Assert.AreEqual(raw16bit, result);
    }

    [TestMethod]
    public void ReturnNull_GivenEmptyDefaultValue()
    {
        Mqtt.PacketDescriptor3 descriptor = default;

        var actual = descriptor.Value;

        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        Mqtt.PacketDescriptor3 descriptor = raw32bit;

        var result = descriptor.Value;

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        Mqtt.PacketDescriptor3 descriptor = raw16bit;

        var result = descriptor.Value;

        Assert.IsNull(result);
    }
}