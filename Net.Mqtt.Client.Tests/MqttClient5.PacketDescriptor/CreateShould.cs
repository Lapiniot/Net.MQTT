using Net.Mqtt.Packets.V5;
using Client5 = Net.Mqtt.Client.MqttClient5;

namespace Net.Mqtt.Client.Tests.MqttClient5.PacketDescriptor;

[TestClass]
public class CreateShould
{
    [TestMethod]
    public void Store_ConnectPacket_GivenPacketValue()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 120, true);
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is ConnectPacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_PublishPacket_GivenPacketValue()
    {
        var packet = new PublishPacket(42, QoSLevel.QoS1, "topic"u8.ToArray(), "payload"u8.ToArray());
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is PublishPacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_PubRelPacket_GivenPacketValue()
    {
        var packet = new PubRelPacket(42, ReasonCode.Success);
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is PubRelPacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_SubscribePacket_GivenPacketValue()
    {
        var packet = new SubscribePacket(42, [("topic"u8.ToArray(), (byte)QoSLevel.QoS1)]);
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is SubscribePacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_UnsubscribePacket_GivenPacketValue()
    {
        var packet = new UnsubscribePacket(42, ["topic"u8.ToArray()]);
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is UnsubscribePacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_UInt32_GivenValidValue_WithPacketHeader()
    {
        const uint raw32bit = PacketFlags.PubAckPacketMask | 0x2A;
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is uint);
        Assert.AreEqual(raw32bit, descriptor);
    }

    [TestMethod]
    public void Store_UInt16_GivenValidValue_WithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is ushort);
        Assert.AreEqual(raw16bit, descriptor);
    }

    [TestMethod]
    public void Store_Null_GivenInvalid32bitValue()
    {
        const uint raw32bit = 0x0000_0001U;
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is null);
    }

    [TestMethod]
    public void Store_Null_GivenInvalid16bitValue()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        var descriptor = Client5.PacketDescriptor.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is null);
    }
}