using static Net.Mqtt.Extensions.SequenceExtensions;

namespace Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadMqttHeaderShould
{
    [TestMethod]
    public void ReturnFalse_GivenEmptySequence()
    {
        var actual = TryReadMqttHeader(in ReadOnlySequence<byte>.Empty, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteContiguousSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205, 255, 255 });

        var actual = TryReadMqttHeader(in sequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteFragmentedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 });

        var actual = TryReadMqttHeader(in sequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenMalformedContiguousSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205, 255, 255, 255, 127, 0 });

        var actual = TryReadMqttHeader(in sequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenMalformedFragmentedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 255, 255 }, new byte[] { 255, 127, 0 });

        var actual = TryReadMqttHeader(in sequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrue_FlagsLengthOffsetOutParams_GivenCompleteContiguousSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205, 255, 255, 127, 0, 0 });

        var actual = TryReadMqttHeader(in sequence, out var actualFlags, out var actualLength, out var actualDataOffset);

        Assert.IsTrue(actual);
        Assert.AreEqual(64, actualFlags);
        Assert.AreEqual(268435405, actualLength);
        Assert.AreEqual(5, actualDataOffset);
    }

    [TestMethod]
    public void ReturnTrue_FlagsLengthOffsetOutParams_GivenCompleteFragmentedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        var actual = TryReadMqttHeader(in sequence, out var actualFlags, out var actualLength, out var actualDataOffset);

        Assert.IsTrue(actual);
        Assert.AreEqual(64, actualFlags);
        Assert.AreEqual(268435405, actualLength);
        Assert.AreEqual(5, actualDataOffset);
    }
}