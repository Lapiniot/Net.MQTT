using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Packets.V5.DisconnectPacket;

namespace System.Net.Mqtt.Tests.V5.DisconnectPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_ReasonCodeAndProperties_GivenContiguousSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0xe0, 0x4c, 0x04, 0x4a, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x1f, 0x00, 0x11,
            0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20, 0x64, 0x69, 0x73, 0x63, 0x6f,
            0x6e, 0x6e, 0x65, 0x63, 0x74, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74,
            0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x26, 0x00,
            0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75,
            0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06,
            0x76, 0x61, 0x6c, 0x75, 0x65, 0x32
        });

        var actual = TryReadPayload(sequence.Slice(2), out var reasonCode, out var sessionExpiryInterval,
            out var reasonString, out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x04, reasonCode);
        Assert.AreEqual(300u, sessionExpiryInterval);
        Assert.IsTrue(reasonString.AsSpan().SequenceEqual("Normal disconnect"u8));
        Assert.IsTrue(serverReference.AsSpan().SequenceEqual("another-server"u8));
        Assert.IsNotNull(properties);
        Assert.AreEqual(2, properties.Count);
        Assert.IsTrue(properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(properties[1].Item2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_ReasonCodeAndProperties_GivenLargerContiguousSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0xe0, 0x4c, 0x04, 0x4a, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x1f, 0x00, 0x11,
            0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20, 0x64, 0x69, 0x73, 0x63, 0x6f,
            0x6e, 0x6e, 0x65, 0x63, 0x74, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74,
            0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x26, 0x00,
            0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75,
            0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06,
            0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        });

        var actual = TryReadPayload(sequence.Slice(2), out var reasonCode, out var sessionExpiryInterval,
            out var reasonString, out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x04, reasonCode);
        Assert.AreEqual(300u, sessionExpiryInterval);
        Assert.IsTrue(reasonString.AsSpan().SequenceEqual("Normal disconnect"u8));
        Assert.IsTrue(serverReference.AsSpan().SequenceEqual("another-server"u8));
        Assert.IsNotNull(properties);
        Assert.AreEqual(2, properties.Count);
        Assert.IsTrue(properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(properties[1].Item2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_ReasonCodeAndProperties_GivenFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0xe0, 0x4c, 0x04, 0x4a, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x1f, 0x00, 0x11,
                0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20, 0x64, 0x69, 0x73, 0x63, 0x6f },
            new byte[] {
                0x6e, 0x6e, 0x65, 0x63, 0x74, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74,
                0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x26, 0x00 },
            new byte[] {
                0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75,
                0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06,
                0x76, 0x61, 0x6c, 0x75, 0x65, 0x32 });

        var actual = TryReadPayload(sequence.Slice(2), out var reasonCode, out var sessionExpiryInterval,
            out var reasonString, out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x04, reasonCode);
        Assert.AreEqual(300u, sessionExpiryInterval);
        Assert.IsTrue(reasonString.AsSpan().SequenceEqual("Normal disconnect"u8));
        Assert.IsTrue(serverReference.AsSpan().SequenceEqual("another-server"u8));
        Assert.IsNotNull(properties);
        Assert.AreEqual(2, properties.Count);
        Assert.IsTrue(properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(properties[1].Item2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_ReasonCodeAndProperties_GivenLargerFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0xe0, 0x4c, 0x04, 0x4a, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x1f, 0x00, 0x11,
                0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20, 0x64, 0x69, 0x73, 0x63, 0x6f },
            new byte[] {
                0x6e, 0x6e, 0x65, 0x63, 0x74, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74,
                0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x26, 0x00 },
            new byte[] {
                0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75,
                0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06,
                0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var actual = TryReadPayload(sequence.Slice(2), out var reasonCode, out var sessionExpiryInterval,
            out var reasonString, out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x04, reasonCode);
        Assert.AreEqual(300u, sessionExpiryInterval);
        Assert.IsTrue(reasonString.AsSpan().SequenceEqual("Normal disconnect"u8));
        Assert.IsTrue(serverReference.AsSpan().SequenceEqual("another-server"u8));
        Assert.IsNotNull(properties);
        Assert.AreEqual(2, properties.Count);
        Assert.IsTrue(properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(properties[1].Item2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_DefaultReasonCodeAndNoProperties_GivenEmptySample()
    {
        var actual = TryReadPayload(ReadOnlySequence<byte>.Empty, out var reasonCode, out var sessionExpiryInterval,
            out var reasonString, out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x00, reasonCode);
        Assert.IsNull(sessionExpiryInterval);
        Assert.IsNull(reasonString);
        Assert.IsNull(serverReference);
        Assert.IsNull(properties);
    }

    [TestMethod]
    public void ReturnTrue_ReasonCodeAndNoProperties_GivenNoPropertiesSample()
    {
        var actual = TryReadPayload(new ReadOnlySequence<byte>(new byte[] { 0x04 }), out var reasonCode, out var sessionExpiryInterval,
            out var reasonString, out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x04, reasonCode);
        Assert.IsNull(sessionExpiryInterval);
        Assert.IsNull(reasonString);
        Assert.IsNull(serverReference);
        Assert.IsNull(properties);
    }

    [TestMethod]
    public void ReturnTrue_ReasonCodeAndNoProperties_GivenNoPropertiesFragmentedSample()
    {
        var actual = TryReadPayload(SequenceFactory.Create<byte>(new byte[] { 0x04 }, Array.Empty<byte>()),
            out var reasonCode, out var sessionExpiryInterval, out var reasonString, out var serverReference, out var properties);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x04, reasonCode);
        Assert.IsNull(sessionExpiryInterval);
        Assert.IsNull(reasonString);
        Assert.IsNull(serverReference);
        Assert.IsNull(properties);
    }

    [TestMethod]
    public void ReturnFalse_GivenContiguousIncompleteSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] {
            0xe0, 0x4c, 0x04, 0x4a, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x1f, 0x00, 0x11,
            0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20, 0x64, 0x69, 0x73, 0x63, 0x6f,
            0x6e, 0x6e, 0x65, 0x63, 0x74, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74,
            0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x26, 0x00
        });

        var actual = TryReadPayload(sequence.Slice(2), out _, out _, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenFragmentedIncompleteSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0xe0, 0x4c, 0x04, 0x4a, 0x11, 0x00, 0x00, 0x01, 0x2c, 0x1f, 0x00, 0x11,
                0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20, 0x64, 0x69, 0x73, 0x63, 0x6f },
            new byte[] {
                0x6e, 0x6e, 0x65, 0x63, 0x74, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74,
                0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x26, 0x00 });

        var actual = TryReadPayload(sequence.Slice(2), out _, out _, out _, out _, out _);

        Assert.IsFalse(actual);
    }
}