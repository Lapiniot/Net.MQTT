using Net.Mqtt.Packets.V3;
using Client3 = Net.Mqtt.Client.MqttClient3Core;

namespace Net.Mqtt.Client.Tests.MqttClient3Core.PacketDescriptor;

[TestClass]
public class TryGetValueShould
{
    [TestMethod]
    public void ReturnPacket_WhenDescriptorContainsPacket()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 4, "MQTT"u8.ToArray(), 120, true);
        Client3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out IMqttPacket? actual);

        Assert.IsTrue(result);
        Assert.AreSame(packet, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingPacket()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out IMqttPacket? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingPacket()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out IMqttPacket? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRawMemoryButRequestingPacket()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        Client3.PacketDescriptor descriptor = topic;

        var result = descriptor.TryGetValue(out IMqttPacket? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnTopic_WhenDescriptorContainsRawMemory()
    {
        ReadOnlyMemory<byte> topic = "testtopic"u8.ToArray();
        Client3.PacketDescriptor descriptor = topic;

        var result = descriptor.TryGetValue(out ReadOnlyMemory<byte> actual);

        Assert.IsTrue(result);
        Assert.AreSequenceEqual(topic, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingTopicMemory()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out ReadOnlyMemory<byte> actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingTopicMemory()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out ReadOnlyMemory<byte> actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingTopicMemory()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 4, "MQTT"u8.ToArray(), 120, true);
        Client3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out ReadOnlyMemory<byte> actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnUInt_WhenDescriptorContainsRaw32bitValue()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsTrue(result);
        Assert.AreEqual(raw32bit, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingUInt32()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 4, "MQTT"u8.ToArray(), 120, true);
        Client3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRawMemoryButRequestingUInt32()
    {
        var topic = new ReadOnlyMemory<byte>([0x00, 0x01]);
        Client3.PacketDescriptor descriptor = topic;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingUInt32()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnUShort_WhenDescriptorContainsRaw16bitValue()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        Client3.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsTrue(result);
        Assert.AreEqual(raw16bit, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingUInt16()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 4, "MQTT"u8.ToArray(), 120, true);
        Client3.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRawMemoryButRequestingUInt16()
    {
        var topic = new ReadOnlyMemory<byte>([0x00, 0x01]);
        Client3.PacketDescriptor descriptor = topic;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingUInt16()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        Client3.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }
}