using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadUInt16Should
{
    private readonly ReadOnlySequence<byte> completeSequence;
    private readonly ReadOnlySequence<byte> emptySequence;
    private readonly ReadOnlySequence<byte> fragmentedSequence;
    private readonly ReadOnlySequence<byte> incompleteSequence;

    public TryReadUInt16Should()
    {
        completeSequence = new(new byte[] { 0x40, 0xCD });
        emptySequence = new(Array.Empty<byte>());
        incompleteSequence = new(new byte[] { 0x40 });
        var segment = new Segment<byte>(new byte[] { 0x40 });
        fragmentedSequence = new(segment, 0, segment.Append(new byte[] { 0xFF }), 1);
    }

    [TestMethod]
    public void ReturnFalseGivenEmptySequence()
    {
        var actual = TryReadUInt16(in emptySequence, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenIncompleteSequence()
    {
        var actual = TryReadUInt16(in incompleteSequence, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenCompleteSequence()
    {
        const int expectedValue = 0x40cd;

        var actual = TryReadUInt16(in completeSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void ReturnTrueGivenFragmentedSequence()
    {
        const int expectedValue = 0x40FF;

        var actual = TryReadUInt16(in fragmentedSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(expectedValue, actualValue);
    }
}