using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadBigEndianShould
{
    [TestMethod]
    public void ReturnFalse_GivenEmptySequence()
    {
        var sequence = new ReadOnlySequence<byte>(Array.Empty<byte>());

        var actual = TryReadBigEndian(in sequence, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteSequence()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] { 0x40 });

        var actual = TryReadBigEndian(in sequence, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteFragmentedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(Array.Empty<byte>(), new byte[] { 0x40 }, Array.Empty<byte>());

        var actual = TryReadBigEndian(in sequence, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteSolidSpanSequence()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] { 0x40, 0xCD });

        var actual = TryReadBigEndian(in sequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40cd, actualValue);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteSolidSpanInterlacedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(Array.Empty<byte>(), new byte[] { 0x40, 0xCD }, Array.Empty<byte>());

        var actual = TryReadBigEndian(in sequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40cd, actualValue);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteFragmentedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 0x40 }, new byte[] { 0xFF });

        var actual = TryReadBigEndian(in sequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40FF, actualValue);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteFragmentedInterlacedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(
            Array.Empty<byte>(), new byte[] { 0x40 }, Array.Empty<byte>(),
            Array.Empty<byte>(), new byte[] { 0xFF });

        var actual = TryReadBigEndian(in sequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40FF, actualValue);
    }
}