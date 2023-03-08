using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

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
    public void ReturnFalse_GivenIncompleteSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 });

        var actual = TryReadMqttHeader(in sequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenWrongSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 255, 127, 0 });

        var actual = TryReadMqttHeader(in sequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        var actual = TryReadMqttHeader(in sequence, out _, out _, out _);

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrue_PacketFlags_GivenCompleteSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        TryReadMqttHeader(in sequence, out var actualFlags, out _, out _);

        Assert.AreEqual(64, actualFlags);
    }

    [TestMethod]
    public void ReturnTrue_RemainingLength_GivenCompleteSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        TryReadMqttHeader(in sequence, out _, out var actualLength, out _);

        Assert.AreEqual(268435405, actualLength);
    }

    [TestMethod]
    public void ReturnTrue_DataOffset_GivenCompleteSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        TryReadMqttHeader(in sequence, out _, out _, out var actualDataOffset);

        Assert.AreEqual(5, actualDataOffset);
    }
}