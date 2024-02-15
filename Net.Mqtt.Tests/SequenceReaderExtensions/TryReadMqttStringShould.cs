using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace Net.Mqtt.Tests.SequenceReaderExtensions;

[TestClass]
public class TryReadMqttStringShould
{
    [TestMethod]
    public void ReturnTrue_ReadString_GivenContiguousSequenceFromStart()
    {
        var reader = new SequenceReader<byte>(new([0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0, 0xb5, 0xd1, 0x81, 0xd1, 0x82]));

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsTrue(actual);
        Assert.IsTrue(value.AsSpan().SequenceEqual("TestString-Тест"u8));
        Assert.AreEqual(21, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_ReadString_GivenContiguousSequence()
    {
        var reader = new SequenceReader<byte>(new([0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0, 0xb5, 0xd1, 0x81, 0xd1, 0x82]));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsTrue(actual);
        Assert.IsTrue(value.AsSpan().SequenceEqual("TestString-Тест"u8));
        Assert.AreEqual(29, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_ReadString_GivenFragmentedSequenceType1()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00, 0x13, 0x54, 0x65,
                0x73, 0x74, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0,
                0xb5, 0xd1, 0x81, 0xd1, 0x82 },
            new byte[] { 0x00, 0x01, 0x02, 0x03 }));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsTrue(actual);
        Assert.IsTrue(value.AsSpan().SequenceEqual("TestString-Тест"u8));
        Assert.AreEqual(29, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_ReadString_GivenFragmentedSequenceType2()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03 },
            new byte[] {
                0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74, 0x72,
                0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0, 0xb5, 0xd1,
                0x81, 0xd1, 0x82 },
            new byte[] { 0x00, 0x01, 0x02, 0x03 }));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsTrue(actual);
        Assert.IsTrue(value.AsSpan().SequenceEqual("TestString-Тест"u8));
        Assert.AreEqual(29, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_ReadString_GivenFragmentedSequenceType3()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00, 0x13 },
            new byte[] {
                0x54, 0x65, 0x73, 0x74, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67,
                0x2d, 0xd0, 0xa2, 0xd0, 0xb5, 0xd1, 0x81, 0xd1, 0x82 },
            new byte[] { 0x00, 0x01, 0x02, 0x03 }));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsTrue(actual);
        Assert.IsTrue(value.AsSpan().SequenceEqual("TestString-Тест"u8));
        Assert.AreEqual(29, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_ReadString_GivenFragmentedSequenceType4()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00 },
            new byte[] {
                0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74, 0x72, 0x69, 0x6e,
                0x67, 0x2d, 0xd0, 0xa2, 0xd0, 0xb5, 0xd1, 0x81, 0xd1, 0x82 },
            new byte[] { 0x00, 0x01, 0x02, 0x03 }));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsTrue(actual);
        Assert.IsTrue(value.AsSpan().SequenceEqual("TestString-Тест"u8));
        Assert.AreEqual(29, reader.Consumed);
    }

    [TestMethod]
    public void ReturnTrue_ReadString_GivenFragmentedSequenceType5()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00 },
            new byte[] { 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74 },
            new byte[] { 0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0, 0xb5, 0xd1, 0x81, 0xd1, 0x82 },
            new byte[] { 0x00, 0x01, 0x02, 0x03 }));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsTrue(actual);
        Assert.IsTrue(value.AsSpan().SequenceEqual("TestString-Тест"u8));
        Assert.AreEqual(29, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_StringNull_GivenEmptySequence()
    {
        var reader = new SequenceReader<byte>(ReadOnlySequence<byte>.Empty);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsFalse(actual);
        Assert.IsNull(value);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_StringNull_GivenIncompleteContiguousSequenceFromStart()
    {
        var reader = new SequenceReader<byte>(new([0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0]));

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsFalse(actual);
        Assert.IsNull(value);
        Assert.AreEqual(0, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_StringNull_GivenIncompleteContiguousSequence()
    {
        var reader = new SequenceReader<byte>(new([0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0]));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsFalse(actual);
        Assert.IsNull(value);
        Assert.AreEqual(8, reader.Consumed);
    }

    [TestMethod]
    public void ReturnFalse_StringNull_GivenIncompleteFragmentedSequence()
    {
        var reader = new SequenceReader<byte>(SequenceFactory.Create<byte>(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00 },
            new byte[] { 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74 },
            new byte[] { 0x72, 0x69, 0x6e, 0x67 }));

        reader.Advance(8);

        var actual = TryReadMqttString(ref reader, out var value);

        Assert.IsFalse(actual);
        Assert.IsNull(value);
        Assert.AreEqual(8, reader.Consumed);
    }
}