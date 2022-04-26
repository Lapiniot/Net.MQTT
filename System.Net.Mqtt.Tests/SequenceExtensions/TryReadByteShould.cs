using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadByteShould
{
    private readonly ReadOnlySequence<byte> completeSequence;
    private readonly ReadOnlySequence<byte> emptySequence;
    private readonly ReadOnlySequence<byte> fragmentedSequence;

    public TryReadByteShould()
    {
        completeSequence = new(new byte[] { 0x40 });
        emptySequence = new(Array.Empty<byte>());
        var segment = new Segment<byte>(Array.Empty<byte>());
        fragmentedSequence = new(segment, 0, segment.Append(new byte[] { 0x40 }), 1);
    }

    [TestMethod]
    public void ReturnFalseGivenEmptySequence()
    {
        var actual = TryReadByte(in emptySequence, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenCompleteSequence()
    {
        const int expectedValue = 0x40;

        var actual = TryReadByte(in completeSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void ReturnTrueGivenFragmentedSequence()
    {
        const int expectedValue = 0x40;

        var actual = TryReadByte(in fragmentedSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(expectedValue, actualValue);
    }
}