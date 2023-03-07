using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadMqttHeaderShould
{
    [TestMethod]
    public void ReturnFalseGivenEmptySequence()
    {
        var emptySequence = new ReadOnlySequence<byte>();

        var actual = TryReadMqttHeader(in emptySequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenIncompleteSequence()
    {
        var incompleteSequence = SequenceFactory.Create(new byte[] { 64, 205 }, new byte[] { 255, 255 });

        var actual = TryReadMqttHeader(in incompleteSequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenWrongSequence()
    {
        var wrongSequence = SequenceFactory.Create(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 255, 127, 0 });

        var actual = TryReadMqttHeader(in wrongSequence, out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenCompleteSequence()
    {
        var completeSequence = SequenceFactory.Create(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        var actual = TryReadMqttHeader(in completeSequence, out _, out _, out _);

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnPacketFlags64GivenCompleteSequence()
    {
        var completeSequence = SequenceFactory.Create(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        TryReadMqttHeader(in completeSequence, out var actualFlags, out _, out _);

        Assert.AreEqual(64, actualFlags);
    }

    [TestMethod]
    public void ReturnLength268435405GivenCompleteSequence()
    {
        var completeSequence = SequenceFactory.Create(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        TryReadMqttHeader(in completeSequence, out _, out var actualLength, out _);

        Assert.AreEqual(268435405, actualLength);
    }

    [TestMethod]
    public void ReturnDataOffset5GivenCompleteSequence()
    {
        var completeSequence = SequenceFactory.Create(new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 });

        TryReadMqttHeader(in completeSequence, out _, out _, out var actualDataOffset);

        Assert.AreEqual(5, actualDataOffset);
    }
}