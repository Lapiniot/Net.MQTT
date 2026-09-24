using Net.Mqtt.Packets.V3;
using MqttSession3 = Net.Mqtt.Server.Protocol.V3.MqttServerSession3;

namespace Net.Mqtt.Server.Tests.MqttServerSession3.PacketDescriptor;

[TestClass]
public class CreateShould
{
    [TestMethod]
    public void Store_SubAckPacket_WithCorrectTag_GivenNonNullValue()
    {
        var packet = new SubAckPacket(7, [0x00, 0x01]);
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is SubAckPacket);
        Assert.AreEqual(packet, descriptor);
        Assert.AreEqual((uint)PacketType.SUBACK, descriptor.Tag);
    }

    [TestMethod]
    public void Store_Topic_WithCorrectValue_GivenNonEmptyValue()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(topic);

        Assert.IsTrue(descriptor.TryGetValue(out ReadOnlyMemory<byte> actual));
        Assert.AreSequenceEqual(topic, actual);
        Assert.IsEmpty(descriptor.Payload);
        Assert.AreEqual(0, descriptor.Id);
        Assert.AreEqual(0, descriptor.Flags);
    }

    [TestMethod]
    public void Store_Topic_WithCorrectValue_GivenNonEmptyValueAndExtraArguments()
    {
        var topic = "testtopic"u8.ToArray();
        var payload = "some data"u8.ToArray();
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(42, topic, payload, PacketFlags.Retain);

        Assert.IsTrue(descriptor.TryGetValue(out ReadOnlyMemory<byte> actual));
        Assert.AreSequenceEqual(topic, actual);
        Assert.AreSequenceEqual(payload, descriptor.Payload);
        Assert.AreEqual(42, descriptor.Id);
        Assert.AreEqual((uint)PacketFlags.Retain, descriptor.Flags);
    }

    [TestMethod]
    public void Store_UInt32_GivenValidValue_WithPacketHeader()
    {
        const uint raw32bit = PacketFlags.UnsubAckPacketMask | 0x2A;
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is uint);
        Assert.AreEqual(raw32bit, descriptor);
    }

    [TestMethod]
    public void Store_UInt16_GivenValidValue_WithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is ushort);
        Assert.AreEqual(raw16bit, descriptor);
    }

    [TestMethod]
    public void Store_Null_GivenEmptyTopic()
    {
        var topic = ReadOnlyMemory<byte>.Empty;
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(topic);

        Assert.IsTrue(descriptor is null);
    }

    [TestMethod]
    public void Store_Null_GivenInvalid32bitValue()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is null);
    }

    [TestMethod]
    public void Store_Null_GivenInvalid16bitValue()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is null);
    }
}