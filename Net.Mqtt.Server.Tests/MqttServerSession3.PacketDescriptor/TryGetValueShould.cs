using Net.Mqtt.Packets.V3;
using MqttSession3 = Net.Mqtt.Server.Protocol.V3.MqttServerSession3;

namespace Net.Mqtt.Server.Tests.MqttServerSession3.PacketDescriptor;

[TestClass]
public class TryGetValueShould
{
    [TestMethod]
    public void ReturnPacket_WhenDescriptorContainsPacket()
    {
        var packet = new SubAckPacket(0x2A, [0x00, 0x01]);
        MqttSession3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out IMqttPacketWriter? actual);

        Assert.IsTrue(result);
        Assert.AreSame(packet, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingPacket()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out IMqttPacketWriter? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingPacket()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out IMqttPacketWriter? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRawMemoryButRequestingPacket()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        MqttSession3.PacketDescriptor descriptor = (topic, ReadOnlyMemory<byte>.Empty, 0);

        var result = descriptor.TryGetValue(out IMqttPacketWriter? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnPublishBlock_WhenDescriptorContainsRawMemory()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        ReadOnlyMemory<byte> payload = "testpayload"u8.ToArray();
        MqttSession3.PacketDescriptor descriptor = (topic, payload, 42u);

        var result = descriptor.TryGetValue(out PublishBlock actual);

        Assert.IsTrue(result);
        Assert.AreSequenceEqual(topic, actual.Topic);
        Assert.AreSequenceEqual(payload, actual.Payload);
        Assert.AreEqual(42u, actual.Flags);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingTopicMemory()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out PublishBlock actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingTopicMemory()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out PublishBlock actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingTopicMemory()
    {
        var packet = new SubAckPacket(42, [0x00, 0x01]);
        MqttSession3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out PublishBlock actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnUInt_WhenDescriptorContainsRaw32bitValue()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsTrue(result);
        Assert.AreEqual(raw32bit, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingUInt32()
    {
        var packet = new SubAckPacket(42, [0x00, 0x01]);
        MqttSession3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRawMemoryButRequestingUInt32()
    {
        var topic = new ReadOnlyMemory<byte>([0x00, 0x01]);
        MqttSession3.PacketDescriptor descriptor = (topic, ReadOnlyMemory<byte>.Empty, 0);

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingUInt32()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnUShort_WhenDescriptorContainsRaw16bitValue()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsTrue(result);
        Assert.AreEqual(raw16bit, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingUInt16()
    {
        var packet = new SubAckPacket(42, [0x00, 0x01]);
        MqttSession3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRawMemoryButRequestingUInt16()
    {
        var topic = new ReadOnlyMemory<byte>([0x00, 0x01]);
        MqttSession3.PacketDescriptor descriptor = (topic, ReadOnlyMemory<byte>.Empty, 0);

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingUInt16()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }
}
