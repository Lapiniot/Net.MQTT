using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Tests.SequenceReaderExtensions;

[TestClass]
public class TryReadMqttHeaderShould
{
    [TestMethod]
    public void ReturnFalse_GivenEmptySequence()
    {
        var reader = new SequenceReader<byte>(new());

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, header);
        Assert.AreEqual(0, length);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteOneByteSequence()
    {
        var reader = new SequenceReader<byte>(new([64]));

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, header);
        Assert.AreEqual(0, length);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteSequence()
    {
        var reader = new SequenceReader<byte>(new([64, 205, 255, 255]));

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, header);
        Assert.AreEqual(0, length);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteFragmentedSequence()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(new byte[] { 64, 205 }, new byte[] { 255, 255 }));

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, header);
        Assert.AreEqual(0, length);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_GivenWrongSequence()
    {
        var reader = new SequenceReader<byte>(new([64, 205, 255, 255, 255, 127, 0]));

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, header);
        Assert.AreEqual(0, length);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_GivenWrongFragmentedSequence()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 255, 127, 0 }));

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, header);
        Assert.AreEqual(0, length);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteSequence()
    {
        var reader = new SequenceReader<byte>(new([64, 205, 255, 255, 127, 0, 0]));

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40, header);
        Assert.AreEqual(0x0fffffcd, length);
        Assert.AreEqual(5, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteFragmentedSequence()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] { 64, 205 }, new byte[] { 255, 255 }, new byte[] { 127, 0, 0 }));

        var actual = TryReadMqttHeader(ref reader, out var header, out var length);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40, header);
        Assert.AreEqual(0x0fffffcd, length);
        Assert.AreEqual(5, reader.Consumed);
    }
}