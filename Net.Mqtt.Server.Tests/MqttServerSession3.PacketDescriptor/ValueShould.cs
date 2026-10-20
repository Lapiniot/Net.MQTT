using Net.Mqtt.Packets.V3;
using MqttSession3 = Net.Mqtt.Server.Protocol.V3.MqttServerSession3;

namespace Net.Mqtt.Server.Tests.MqttServerSession3.PacketDescriptor;

[TestClass]
public class ValueShould
{
    [TestMethod]
    public void ReturnPublishBlock_GivenTopicValue()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        ReadOnlyMemory<byte> payload = "testpayload"u8.ToArray();
        MqttSession3.PacketDescriptor descriptor = (topic, payload, 42u);

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
        MqttSession3.PacketDescriptor descriptor = packet;

        var result = descriptor.Value;

        Assert.AreSame(packet, result);
    }

    [TestMethod]
    public void ReturnUInt32_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.Value;

        Assert.IsTrue(result is uint);
        Assert.AreEqual(raw32bit, result);
    }

    [TestMethod]
    public void ReturnUInt16_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.Value;

        Assert.IsTrue(result is ushort);
        Assert.AreEqual(raw16bit, result);
    }

    [TestMethod]
    public void ReturnNull_GivenEmptyDefaultValue()
    {
        MqttSession3.PacketDescriptor descriptor = default;

        var actual = descriptor.Value;

        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.Value;

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.Value;

        Assert.IsNull(result);
    }
}