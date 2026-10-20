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
    public void Store_PublishBlock_GivenNonEmptyTopicValueAndExtraArguments()
    {
        var topic = "testtopic"u8.ToArray();
        var payload = "some data"u8.ToArray();
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create((topic, payload, 42));

        Assert.IsTrue(descriptor.TryGetValue(out PublishBlock actual));
        Assert.AreSequenceEqual(topic, actual.Topic);
        Assert.AreSequenceEqual(payload, actual.Payload);
        Assert.AreEqual(42u, actual.Flags);
    }

    [TestMethod]
    public void Store_PublishBlock_GivenMinimumNonEmptyTopicAndDefaultOtherArguments()
    {
        ReadOnlyMemory<byte> topic = new byte[] { 0x01 };
        var descriptor = MqttSession3.PacketDescriptor.IUnionMembers.Create((topic, ReadOnlyMemory<byte>.Empty, 0));

        Assert.IsTrue(descriptor.TryGetValue(out PublishBlock actual));
        Assert.AreSequenceEqual(new byte[] { 0x01 }, actual.Topic);
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
    public void ThrowArgumentOutOfRangeException_GivenDefaultEmptyTopic()
    {
        var topic = ReadOnlyMemory<byte>.Empty;

        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => MqttSession3.PacketDescriptor.IUnionMembers.Create((topic, ReadOnlyMemory<byte>.Empty, 0)));

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
                return MqttSession3.PacketDescriptor.IUnionMembers.Create((
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