using static Net.Mqtt.Packets.V5.DisconnectPacket;

namespace Net.Mqtt.Tests.V5.DisconnectPacket;

[TestClass]
public class TryReadPropertiesSpanShould
{
    [TestMethod]
    public void ReturnTrue_Properties_GivenSample()
    {
        var span = new ReadOnlySpan<byte>(
        [
            0x11, 0x00, 0x00, 0x01, 0x2c,
            0x1f, 0x00, 0x11,
            0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20, 0x64, 0x69, 0x73,
            0x63, 0x6f, 0x6e, 0x6e, 0x65, 0x63, 0x74,
            0x1c, 0x00, 0x0e,
            0x61, 0x6e, 0x6f, 0x74, 0x68, 0x65, 0x72, 0x2d, 0x73, 0x65,
            0x72, 0x76, 0x65, 0x72,
            0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31,
            0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31,
            0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32,
            0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32,
        ]);

        var actual = TryReadProperties(span, out var sessionExpiryInterval, out var reasonString,
            out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(300u, sessionExpiryInterval);
        CollectionAssert.AreEqual("Normal disconnect"u8, reasonString);
        CollectionAssert.AreEqual("another-server"u8, serverReference);
        Assert.IsNotNull(properties);
        Assert.AreEqual(2, properties.Count);
        CollectionAssert.AreEqual("prop1"u8, properties[0].Name.Span);
        CollectionAssert.AreEqual("value1"u8, properties[0].Value.Span);
        CollectionAssert.AreEqual("prop2"u8, properties[1].Name.Span);
        CollectionAssert.AreEqual("value2"u8, properties[1].Value.Span);
    }

    [TestMethod]
    public void ReturnFalse_GivenUnknownPropertyIdentifier()
    {
        var span = new ReadOnlySpan<byte>([0x99]);

        var actual = TryReadProperties(span, out var sessionExpiryInterval, out var reasonString,
            out var serverReference, out var properties);

        Assert.IsFalse(actual);
        Assert.IsNull(sessionExpiryInterval);
        Assert.IsNull(reasonString);
        Assert.IsNull(serverReference);
        Assert.IsNull(properties);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteUserPropertyValue()
    {
        var span = new ReadOnlySpan<byte>(
        [
            0x26,
            0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31,
        ]);

        var actual = TryReadProperties(span, out var sessionExpiryInterval, out var reasonString,
            out var serverReference, out var properties);

        Assert.IsFalse(actual);
        Assert.IsNull(sessionExpiryInterval);
        Assert.IsNull(reasonString);
        Assert.IsNull(serverReference);
        Assert.IsNull(properties);
    }

    [TestMethod]
    public void ReturnTrue_EmptyProperties_GivenNoUserProperties()
    {
        var actual = TryReadProperties([], out var sessionExpiryInterval, out var reasonString,
            out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.IsNull(sessionExpiryInterval);
        Assert.IsNull(reasonString);
        Assert.IsNull(serverReference);
        Assert.IsNotNull(properties);
        Assert.AreEqual(0, properties.Count);
    }
}