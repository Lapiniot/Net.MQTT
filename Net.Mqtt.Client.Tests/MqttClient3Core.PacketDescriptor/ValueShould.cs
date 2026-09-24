using Net.Mqtt.Packets.V3;
using Client3 = Net.Mqtt.Client.MqttClient3Core;

namespace Net.Mqtt.Client.Tests.MqttClient3Core.PacketDescriptor;

[TestClass]
public class ValueShould
{
    [TestMethod]
    public void ReturnTopic_GivenTopicValue()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        Client3.PacketDescriptor descriptor = topic;

        var actual = descriptor.Value;

        Assert.IsNotNull(actual);
        Assert.AreSequenceEqual(topic, (ReadOnlyMemory<byte>)actual);
    }

    [TestMethod]
    public void ReturnPacket_GivenPacketDescriptorContainingPacket()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 4, "MQTT"u8.ToArray(), 120, true);
        Client3.PacketDescriptor descriptor = packet;

        var actual = descriptor.Value;

        Assert.AreSame(packet, actual);
    }

    [TestMethod]
    public void ReturnUInt32_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client3.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.Value;

        Assert.IsTrue(actual is uint);
        Assert.AreEqual(raw32bit, actual);
    }

    [TestMethod]
    public void ReturnUInt16_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client3.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.Value;

        Assert.IsTrue(actual is ushort);
        Assert.AreEqual(raw16bit, actual);
    }

    [TestMethod]
    public void ReturnNull_GivenEmptyMemoryValue()
    {
        var empty = ReadOnlyMemory<byte>.Empty;
        Client3.PacketDescriptor descriptor = empty;

        var actual = descriptor.Value;

        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0x0000_0001U;
        Client3.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.Value;

        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnNull_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        Client3.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.Value;

        Assert.IsNull(actual);
    }
}