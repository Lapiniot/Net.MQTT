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
    public void Store_PublishBlock_GivenNonEmptyTopicValueAndExtraArguments()
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
    public void Store_PublishBlock_GivenMinimumNonEmptyTopicAndDefaultOtherArguments()
    {
        ReadOnlyMemory<byte> topic = new byte[] { 0x01 };
        var descriptor = Client3.PacketDescriptor.IUnionMembers.Create((topic, ReadOnlyMemory<byte>.Empty, 0));

        Assert.IsTrue(descriptor.TryGetValue(out PublishBlock actual));
        Assert.AreSequenceEqual(new byte[] { 0x01 }, actual.Topic);
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
    public void ThrowArgumentOutOfRangeException_GivenDefaultEmptyTopic()
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Client3.PacketDescriptor.IUnionMembers.Create((
                Topic: ReadOnlyMemory<byte>.Empty,
                Payload: ReadOnlyMemory<byte>.Empty,
                Flags: 0)));

        Assert.AreEqual("Topic", exception.ParamName);
    }

    [TestMethod]
    [Description("This test is to ensure that the PacketDescriptor struct cannot be initialized with arbitrary non default zero-length topic." +
        "Default (returned by default(System.ReadOnlyMemory<byte>)) value and general case zero length memory value (returned by memory.Slice(0,0) e.g) " +
        "are not the same, eventhough they may represent the same empty memory block conceptually. Default value has its internal fields all set to defaults, " +
        "whereas arbitrary zero-length memory value may have its _object field set to a non-null value, confusing our stored type detection logic, " +
        "so it incorrectly detects stored value as the reference to an IMqttPacket instance.")]
    public void ThrowArgumentOutOfRangeException_GivenArbitraryZeroLengthTopic()
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () =>
            {
                var zeroLengthMemory = "data"u8.ToArray().AsMemory().Slice(0, 0);
                return Client3.PacketDescriptor.IUnionMembers.Create((
                                Topic: zeroLengthMemory,
                                Payload: ReadOnlyMemory<byte>.Empty,
                                Flags: 0));
            });

        Assert.AreEqual("Topic", exception.ParamName);
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