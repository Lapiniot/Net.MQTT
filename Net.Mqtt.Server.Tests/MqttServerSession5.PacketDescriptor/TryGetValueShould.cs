using Net.Mqtt.Packets.V5;
using MqttSession5 = Net.Mqtt.Server.Protocol.V5.MqttServerSession5;

namespace Net.Mqtt.Server.Tests.MqttServerSession5.PacketDescriptor;

[TestClass]
public class TryGetValueShould
{
    [TestMethod]
    public void ReturnPacket_WhenDescriptorContainsPacket()
    {
        var packet = new PubAckPacket(0x2A);
        MqttSession5.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out IMqttPacket5? actual);

        Assert.IsTrue(result);
        Assert.AreSame(packet, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingPacket()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession5.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out IMqttPacket5? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingPacket()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession5.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out IMqttPacket5? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnPublishPacket_WhenDescriptorContainsPublishPacket()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        MqttSession5.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out PublishPacket? actual);

        Assert.IsTrue(result);
        Assert.AreSame(packet, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsDifferentPacketType()
    {
        var packet = new PubAckPacket(7);
        MqttSession5.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out PublishPacket? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingPublishPacket()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession5.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out PublishPacket? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingPublishPacket()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession5.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out PublishPacket? actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnUInt_WhenDescriptorContainsRaw32bitValue()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession5.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsTrue(result);
        Assert.AreEqual(raw32bit, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingUInt32()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        MqttSession5.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw16bitValueButRequestingUInt32()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession5.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out uint actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnUShort_WhenDescriptorContainsRaw16bitValue()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        MqttSession5.PacketDescriptor descriptor = raw16bit;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsTrue(result);
        Assert.AreEqual(raw16bit, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsPacketButRequestingUInt16()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        MqttSession5.PacketDescriptor descriptor = packet;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }

    [TestMethod]
    public void ReturnFalse_WhenDescriptorContainsRaw32bitValueButRequestingUInt16()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        MqttSession5.PacketDescriptor descriptor = raw32bit;

        var result = descriptor.TryGetValue(out ushort actual);

        Assert.IsFalse(result);
        Assert.AreEqual(default, actual);
    }
}