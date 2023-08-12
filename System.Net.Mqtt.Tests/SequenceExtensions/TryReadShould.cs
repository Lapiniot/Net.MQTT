using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadShould
{
    [TestMethod]
    public void ReturnFalse_GivenEmptySequence()
    {
        var actual = TryRead(in ReadOnlySequence<byte>.Empty, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrue_GivenCompleteSequence()
    {
        var sequence = new ReadOnlySequence<byte>([0x40]);

        var actual = TryRead(in sequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40, actualValue);
    }

    [TestMethod]
    public void ReturnTrue_GivenFragmentedSequence()
    {
        var sequence = SequenceFactory.Create<byte>(Array.Empty<byte>(), Array.Empty<byte>(), new byte[] { 0x40 }, Array.Empty<byte>());

        var actual = TryRead(in sequence, out var actualValue);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x40, actualValue);
    }
}