using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadMqttStringShould
{
    private readonly ReadOnlySequence<byte> completeSequence;
    private readonly ReadOnlySequence<byte> emptySequence;
    private readonly ReadOnlySequence<byte> fragmentedSequence;
    private readonly ReadOnlySequence<byte> incompleteSequence;

    public TryReadMqttStringShould()
    {
        completeSequence = new(new byte[]
        {
            0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66,
            0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0,
            0xb3, 0xd0, 0xb4, 0xd0, 0xb5
        });

        emptySequence = new(Array.Empty<byte>());

        incompleteSequence = new(new byte[]
        {
            0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66,
            0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0
        });

        var segment1 = new Segment<byte>(new byte[] { 0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 });
        var segment2 = segment1
            .Append(new byte[] { 0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0 })
            .Append(new byte[] { 0xb3, 0xd0, 0xb4, 0xd0, 0xb5 });
        fragmentedSequence = new(segment1, 0, segment2, 5);
    }

    [TestMethod]
    public void ReturnFalseGivenEmptySequence()
    {
        var actual = TryReadMqttString(in emptySequence, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenIncompleteSequence()
    {
        var actual = TryReadMqttString(in incompleteSequence, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenCompleteSequence()
    {
        U8 expectedValue = "abcdef-абвгде";

        var actual = TryReadMqttString(in completeSequence, out var actualValue, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsTrue(expectedValue.SequenceEqual(actualValue));
        Assert.AreEqual(21, consumed);
    }

    [TestMethod]
    public void ReturnTrueGivenFragmentedSequence()
    {
        U8 expectedValue = "abcdef-абвгде";

        var actual = TryReadMqttString(in fragmentedSequence, out var actualValue, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsTrue(expectedValue.SequenceEqual(actualValue));
        Assert.AreEqual(21, consumed);
    }
}