using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SequenceExtensions;

[TestClass]
public class TryReadMqttHeaderShould
{
    [TestMethod]
    public void ReturnFalseGivenEmptySequence()
    {
        var actual = new ReadOnlySequence<byte>().TryReadMqttHeader(out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenIncompleteSequence()
    {
        var segment = new Segment<byte>(new byte[] { 64, 205 });
        var incompleteSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] { 255, 255 }), 2);

        var actual = incompleteSequence.TryReadMqttHeader(out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenWrongSequence()
    {
        var segment = new Segment<byte>(new byte[] { 64, 205 });
        var wrongSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] { 255, 255 }).Append(new byte[] { 255, 127, 0 }), 3);

        var actual = wrongSequence.TryReadMqttHeader(out _, out _, out _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenCompleteSequence()
    {
        var segment = new Segment<byte>(new byte[] { 64, 205 });
        var completeSequence = new ReadOnlySequence<byte>(segment, 0,
            segment.Append(new byte[] { 255, 255 }).Append(new byte[] { 127, 0, 0 }), 3);

        var actual = completeSequence.TryReadMqttHeader(out _, out _, out _);

        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnPacketFlags64GivenCompleteSequence()
    {
        const int expectedFlags = 64;

        var segment = new Segment<byte>(new byte[] { 64, 205 });
        var completeSequence = new ReadOnlySequence<byte>(segment, 0,
            segment.Append(new byte[] { 255, 255 }).Append(new byte[] { 127, 0, 0 }), 3);

        completeSequence.TryReadMqttHeader(out var actualFlags, out _, out _);

        Assert.AreEqual(expectedFlags, actualFlags);
    }

    [TestMethod]
    public void ReturnLength268435405GivenCompleteSequence()
    {
        const int expectedLength = 268435405;

        var segment = new Segment<byte>(new byte[] { 64, 205 });
        var completeSequence = new ReadOnlySequence<byte>(segment, 0,
            segment.Append(new byte[] { 255, 255 }).Append(new byte[] { 127, 0, 0 }), 3);

        completeSequence.TryReadMqttHeader(out _, out var actualLength, out _);

        Assert.AreEqual(expectedLength, actualLength);
    }

    [TestMethod]
    public void ReturnDataOffset5GivenCompleteSequence()
    {
        const int expectedDataOffset = 5;

        var segment = new Segment<byte>(new byte[] { 64, 205 });
        var completeSequence = new ReadOnlySequence<byte>(segment, 0,
            segment.Append(new byte[] { 255, 255 }).Append(new byte[] { 127, 0, 0 }), 3);

        completeSequence.TryReadMqttHeader(out _, out _, out var actualDataOffset);

        Assert.AreEqual(expectedDataOffset, actualDataOffset);
    }
}