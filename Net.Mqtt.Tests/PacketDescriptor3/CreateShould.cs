using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Tests.PacketDescriptor3;

[TestClass]
public class CreateShould
{
    [TestMethod]
    public void Store_ArbitraryPacket_GivenValidPacketInstance()
    {
        var packet = new SubAckPacket(7, [0x00, 0x01]);
        var descriptor = Mqtt.PacketDescriptor3.IUnionMembers.Create(packet);

        Assert.IsTrue(descriptor is SubAckPacket);
        Assert.AreEqual(packet, descriptor);
    }

    [TestMethod]
    public void Store_PublishBlock_GivenNonEmptyTopicValueAndExtraArguments()
    {
        var topic = "testtopic"u8.ToArray();
        var payload = "some data"u8.ToArray();
        var descriptor = Mqtt.PacketDescriptor3.IUnionMembers.Create((topic, payload, 42));

        Assert.IsTrue(descriptor.TryGetValue(out PublishBlock actual));
        Assert.AreSequenceEqual(topic, actual.Topic);
        Assert.AreSequenceEqual(payload, actual.Payload);
        Assert.AreEqual(42u, actual.Flags);
    }

    [TestMethod]
    public void Store_PublishBlock_GivenMinimumNonEmptyTopicAndDefaultOtherArguments()
    {
        ReadOnlyMemory<byte> topic = new byte[] { 0x01 };
        var descriptor = Mqtt.PacketDescriptor3.IUnionMembers.Create((topic, ReadOnlyMemory<byte>.Empty, 0));

        Assert.IsTrue(descriptor.TryGetValue(out PublishBlock actual));
        Assert.AreSequenceEqual(new byte[] { 0x01 }, actual.Topic);
    }

    [TestMethod]
    [Description("Verify that valid raw 4-bytes sized packet data is stored correctly as a UInt32 value. " +
        "Value bits 28-31 encoding non-zero packet type, is considered a valid packet.")]
    public void Store_UInt32_GivenValidValue_WithPacketHeader()
    {
        const uint raw32bit = PacketFlags.UnsubAckPacketMask | 0x2A;
        var descriptor = Mqtt.PacketDescriptor3.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is uint);
        Assert.AreEqual(raw32bit, descriptor);
    }

    [TestMethod]
    [Description("Verify that valid raw 2-bytes sized packet data is stored correctly as a UInt16 value. " +
        "Value bits 12-15 encoding non-zero packet type, is considered a valid packet.")]
    public void Store_UInt16_GivenValidValue_WithPacketHeader()
    {
        const ushort raw16bit = PacketFlags.PingRespPacket;
        var descriptor = Mqtt.PacketDescriptor3.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is ushort);
        Assert.AreEqual(raw16bit, descriptor);
    }

    [TestMethod]
    [Description("Verify that the PacketDescriptor struct throws an ArgumentOutOfRangeException when initialized with a default empty topic.")]
    public void ThrowArgumentOutOfRangeException_GivenDefaultEmptyTopic()
    {
        var topic = ReadOnlyMemory<byte>.Empty;

        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Mqtt.PacketDescriptor3.IUnionMembers.Create((topic, ReadOnlyMemory<byte>.Empty, 0)));

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
                var zeroLengthMemory = "data"u8.ToArray().AsMemory()[..0];
                return Mqtt.PacketDescriptor3.IUnionMembers.Create((
                                Topic: zeroLengthMemory,
                                Payload: ReadOnlyMemory<byte>.Empty,
                                Flags: 0));
            });

        Assert.AreEqual("Topic", exception.ParamName);
    }

    [TestMethod]
    [Description("Verify that invalid raw 4-bytes sized packet data is ignored, so resulting value evaluates to null. " +
        "Bits 28-31 should encode the packet type, otherwise the value is considered invalid.")]
    public void Store_Null_GivenInvalid32bitValue()
    {
        const uint raw32bit = 0b0000_0001_0010_0011_0100_0101_0110_0111U;
        var descriptor = Mqtt.PacketDescriptor3.IUnionMembers.Create(raw32bit);

        Assert.IsTrue(descriptor is null);
    }

    [TestMethod]
    [Description("Verify that invalid raw 2-bytes sized packet data is ignored, so resulting value evaluates to null. " +
        "Bits 12-15 should encode the packet type, otherwise the value is considered invalid.")]
    public void Store_Null_GivenInvalid16bitValue()
    {
        const ushort raw16bit = 0b0000_0010_0011_0100;
        var descriptor = Mqtt.PacketDescriptor3.IUnionMembers.Create(raw16bit);

        Assert.IsTrue(descriptor is null);
    }
}