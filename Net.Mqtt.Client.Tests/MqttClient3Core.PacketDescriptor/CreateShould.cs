using Net.Mqtt.Packets.V3;
using Client3 = Net.Mqtt.Client.MqttClient3Core;

namespace Net.Mqtt.Client.Tests.MqttClient3Core.PacketDescriptor;

[TestClass]
public class CreateShould
{
    [TestMethod]
    public void Store_ConnectPacket_GivenPacketValue()
    {
        var packet = new ConnectPacket("client"u8.ToArray(), 4, "MQTT"u8.ToArray(), 120, true);
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is ConnectPacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_SubscribePacket_GivenPacketValue()
    {
        var packet = new SubscribePacket(7, [("topic"u8.ToArray(), (byte)QoSLevel.QoS1)]);
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is SubscribePacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_UnsubscribePacket_GivenPacketValue()
    {
        var packet = new UnsubscribePacket(8, ["topic"u8.ToArray()]);
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is UnsubscribePacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_Topic_GivenNonEmptyValueAndExtraArguments()
    {
        var topic = "testtopic"u8.ToArray();
        var payload = "some data"u8.ToArray();
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create((topic, payload, 42));

        Assert.IsTrue(descriptor.TryGetValue(out PublishBlock actual));
        Assert.AreSequenceEqual(topic, actual.Topic);
        Assert.AreSequenceEqual(payload, actual.Payload);
        Assert.AreEqual(42u, actual.Flags);
    }

    [TestMethod]
    public void Store_UInt32_GivenValidValue_WithPacketHeader()
    {
        const uint raw32bit = PacketFlags.UnsubAckPacketMask | 0x2A;
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is uint);
        Assert.AreEqual(raw32bit, descriptor);
    }

    [TestMethod]
    public void Store_UInt16_GivenValidValue_WithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is ushort);
        Assert.AreEqual(raw16bit, descriptor);
    }

    [TestMethod]
    public void Store_Null_GivenEmptyTopic()
    {
        var topic = ReadOnlyMemory<byte>.Empty;
        var payload = ReadOnlyMemory<byte>.Empty;
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create((topic, payload, 0));

        Assert.IsTrue(descriptor is null);
    }

    [TestMethod]
    public void Store_Null_GivenInvalid32bitValue()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is null);
    }

    [TestMethod]
    public void Store_Null_GivenInvalid16bitValue()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is null);
    }
}