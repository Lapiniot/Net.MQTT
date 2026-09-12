using static Net.Mqtt.Packets.V5.UnsubscribePacket;

namespace Net.Mqtt.Tests.V5.UnsubscribePacket;

[TestClass]
public class TryReadPropertiesSequenceShould
{
    [TestMethod]
    public void ReturnTrue_UserProperties_GivenValidSample()
    {
        var sequence = new ReadOnlySequence<byte>([
            0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31,
            0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31,
            0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32,
            0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32,
        ]);

        var actual = TryReadProperties(sequence, out var properties);

        Assert.IsTrue(actual);
        Assert.IsNotNull(properties);
        Assert.HasCount(2, properties);
        CollectionAssert.AreEqual("prop1"u8, properties[0].Name.Span);
        CollectionAssert.AreEqual("value1"u8, properties[0].Value.Span);
        CollectionAssert.AreEqual("prop2"u8, properties[1].Name.Span);
        CollectionAssert.AreEqual("value2"u8, properties[1].Value.Span);
    }

    [TestMethod]
    public void ReturnFalse_GivenUnknownPropertyIdentifier()
    {
        var sequence = new ReadOnlySequence<byte>([0x99]);

        var actual = TryReadProperties(sequence, out var properties);

        Assert.IsFalse(actual);
        Assert.IsNull(properties);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteUserPropertyValue()
    {
        var sequence = new ReadOnlySequence<byte>([
            0x26,
            0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31,
        ]);

        var actual = TryReadProperties(sequence, out var properties);

        Assert.IsFalse(actual);
        Assert.IsNull(properties);
    }

    [TestMethod]
    public void ReturnTrue_EmptyProperties_GivenNoUserProperties()
    {
        var actual = TryReadProperties(ReadOnlySequence<byte>.Empty, out var properties);

        Assert.IsTrue(actual);
        Assert.IsNotNull(properties);
        Assert.IsEmpty(properties);
    }
}