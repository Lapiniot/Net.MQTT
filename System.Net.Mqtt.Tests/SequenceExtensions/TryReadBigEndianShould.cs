using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadBigEndianShould
{
    private readonly ReadOnlySequence<byte> completeSequence;
    private readonly ReadOnlySequence<byte> emptySequence;
    private readonly ReadOnlySequence<byte> fragmentedSequence;
    private readonly ReadOnlySequence<byte> fragmentedSequenceIncomplete;
    private readonly ReadOnlySequence<byte> fragmentedSequenceInterlaced;
    private readonly ReadOnlySequence<byte> fragmentedSequenceInterlacedIncomplete;
    private readonly ReadOnlySequence<byte> incompleteSequence;

    public TryReadBigEndianShould()
    {
        completeSequence = new(new byte[] { 0x40, 0xCD });
        emptySequence = new(Array.Empty<byte>());
        incompleteSequence = new(new byte[] { 0x40 });
        fragmentedSequence = SequenceFactory.Create(new byte[] { 0x40 }, new byte[] { 0xFF });
        fragmentedSequenceInterlaced = SequenceFactory.Create(Array.Empty<byte>(), new byte[] { 0x40 }, Array.Empty<byte>(), Array.Empty<byte>(), new byte[] { 0xFF });
        fragmentedSequenceIncomplete = SequenceFactory.Create(new byte[] { 0x40 });
        fragmentedSequenceInterlacedIncomplete = SequenceFactory.Create(Array.Empty<byte>(), new byte[] { 0x40 }, Array.Empty<byte>());
    }

    [TestMethod]
    public void ReturnFalse_GivenEmptySequence()
    {
        var actual = TryReadBigEndian(in emptySequence, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteSequence()
    {
        var actual = TryReadBigEndian(in incompleteSequence, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ReturnFalse_GivenFragmentedIncompleteSequence()
    {
        var actual = TryReadBigEndian(in fragmentedSequenceIncomplete, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ReturnFalse_GivenFragmentedIncompleteInterlacedSequence()
    {
        var actual = TryReadBigEndian(in fragmentedSequenceInterlacedIncomplete, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteSequence()
    {
        var actual = TryReadBigEndian(in completeSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40cd, actualValue);
    }

    [TestMethod]
    public void ReturnTrue_GivenFragmentedSequence()
    {
        var actual = TryReadBigEndian(in fragmentedSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40FF, actualValue);
    }

    [TestMethod]
    public void ReturnTrue_GivenFragmentedInterlacedSequence()
    {
        var actual = TryReadBigEndian(in fragmentedSequenceInterlaced, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40FF, actualValue);
    }
}