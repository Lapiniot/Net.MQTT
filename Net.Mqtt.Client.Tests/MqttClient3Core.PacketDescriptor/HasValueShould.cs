using Net.Mqtt.Packets.V3;
using Client3 = Net.Mqtt.Client.MqttClient3Core;

namespace Net.Mqtt.Client.Tests.MqttClient3Core.PacketDescriptor;

[TestClass]
public class HasValueShould
{
    [TestMethod]
    public void ReturnTrue_GivenPacketDescriptorContainingPacket()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 4, "MQTT"u8.ToArray(), 120, true);
        Client3.PacketDescriptor descriptor = packet;

        var actual = descriptor.HasValue;

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenTopicValue()
    {
        Client3.PacketDescriptor descriptor = (Topic: "testtopic"u8.ToArray().AsMemory(), Payload: "some data"u8.ToArray().AsMemory(), Flags: 42u);

        var actual = descriptor.HasValue;

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenEmptyTopicValue()
    {
        Client3.PacketDescriptor descriptor = (Topic: ReadOnlyMemory<byte>.Empty, Payload: ReadOnlyMemory<byte>.Empty, Flags: 0);

        var actual = descriptor.HasValue;

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt32ValueWithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client3.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.HasValue;

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenRawUInt16ValueWithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client3.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.HasValue;

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt32Value()
    {
        const uint raw32bit = 0x0000_0001U;
        Client3.PacketDescriptor descriptor = raw32bit;

        var actual = descriptor.HasValue;

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenInvalidRawUInt16Value()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        Client3.PacketDescriptor descriptor = raw16bit;

        var actual = descriptor.HasValue;

        Assert.IsFalse(actual);
    }
}