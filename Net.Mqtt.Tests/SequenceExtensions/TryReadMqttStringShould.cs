using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Net.Mqtt.Extensions.SequenceExtensions;

namespace Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadMqttStringShould
{
    [TestMethod]
    public void ReturnFalse_GivenEmptySequence()
    {
        var actual = TryReadMqttString(in ReadOnlySequence<byte>.Empty, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteSequence()
    {
        var sequence = new ReadOnlySequence<byte>([0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0]);

        var actual = TryReadMqttString(in sequence, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteSequence()
    {
        var sequence = new ReadOnlySequence<byte>([0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0, 0xb3, 0xd0, 0xb4, 0xd0, 0xb5]);

        var actual = TryReadMqttString(in sequence, out var actualValue, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsTrue(actualValue.AsSpan().SequenceEqual("abcdef-абвгде"u8));
        Assert.AreEqual(21, consumed);
    }

    [TestMethod]
    public void ReturnTrue_GivenFragmentedSequence()
    {
        var fragmentedSequence = SequenceFactory.Create<byte>(
            new byte[] { 0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 },
            new byte[] { 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0 },
            new byte[] { 0xb3, 0xd0, 0xb4, 0xd0, 0xb5 });

        var actual = TryReadMqttString(in fragmentedSequence, out var actualValue, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsTrue(actualValue.AsSpan().SequenceEqual("abcdef-абвгде"u8));
        Assert.AreEqual(21, consumed);
    }
}