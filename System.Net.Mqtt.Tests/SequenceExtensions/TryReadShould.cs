using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadShould
{
    private readonly ReadOnlySequence<byte> completeSequence;
    private readonly ReadOnlySequence<byte> emptySequence;
    private readonly ReadOnlySequence<byte> fragmentedSequence;

    public TryReadShould()
    {
        completeSequence = new(new byte[] { 0x40 });
        emptySequence = new(Array.Empty<byte>());
        fragmentedSequence = SequenceFactory.Create(Array.Empty<byte>(), new byte[] { 0x40 });
    }

    [TestMethod]
    public void ReturnFalseGivenEmptySequence()
    {
        var actual = TryRead(in emptySequence, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenCompleteSequence()
    {
        var actual = TryRead(in completeSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40, actualValue);
    }

    [TestMethod]
    public void ReturnTrueGivenFragmentedSequence()
    {
        var actual = TryRead(in fragmentedSequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40, actualValue);
    }
}