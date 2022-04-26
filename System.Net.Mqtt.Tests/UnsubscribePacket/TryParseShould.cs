using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.UnsubscribePacket;

[TestClass]
public class TryParseShould
{
    private readonly ReadOnlySequence<byte> emptySample = ReadOnlySequence<byte>.Empty;

    private readonly ReadOnlySequence<byte> fragmentedSample;

    private readonly ReadOnlySequence<byte> incompleteSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65
    }));

    private readonly ReadOnlySequence<byte> largerBufferSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
        0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
        0x69, 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68,
        0x2f
    }));

    private readonly ReadOnlySequence<byte> largerFragmentedSample;

    private readonly ReadOnlySequence<byte> sample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
        0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
        0x69
    }));

    private readonly ReadOnlySequence<byte> wrongTypeSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0x12, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
        0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
        0x69
    }));

    public TryParseShould()
    {
        var segment1 = new Segment<byte>(new byte[] { 0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f });

        var segment2 = segment1
            .Append(new byte[] { 0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65 })
            .Append(new byte[] { 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 });

        fragmentedSample = new(segment1, 0, segment2, 9);

        var segment3 = segment2.Append(new byte[] { 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69 });
        largerFragmentedSample = new(segment1, 0, segment3, 7);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullConsumed25GivenValidSample()
    {
        var actual = Packets.UnsubscribePacket.TryRead(in sample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(25, consumed);
        var topics = packet.Filters;
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0]);
        Assert.AreEqual("d/e/f", topics[1]);
        Assert.AreEqual("g/h/i", topics[2]);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullConsumed25GivenValidFragmentedSample()
    {
        var actual = Packets.UnsubscribePacket.TryRead(in fragmentedSample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(25, consumed);
        var topics = packet.Filters;
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0]);
        Assert.AreEqual("d/e/f", topics[1]);
        Assert.AreEqual("g/h/i", topics[2]);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullConsumed25GivenLargerBufferSample()
    {
        var actual = Packets.UnsubscribePacket.TryRead(in largerBufferSample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(25, consumed);
        var topics = packet.Filters;
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0]);
        Assert.AreEqual("d/e/f", topics[1]);
        Assert.AreEqual("g/h/i", topics[2]);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullConsumed25GivenLargerFragmentedBufferSample()
    {
        var actual = Packets.UnsubscribePacket.TryRead(in largerFragmentedSample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(25, consumed);
        var topics = packet.Filters;
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0]);
        Assert.AreEqual("d/e/f", topics[1]);
        Assert.AreEqual("g/h/i", topics[2]);
    }

    [TestMethod]
    public void ReturnFalsePacketNullConsumed0GivenIncompleteSample()
    {
        var actual = Packets.UnsubscribePacket.TryRead(in incompleteSample, out var packet, out var consumed);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
        Assert.AreEqual(0, consumed);
    }

    [TestMethod]
    public void ReturnFalsePacketNullConsumed0GivenWrongTypeSample()
    {
        var actual = Packets.UnsubscribePacket.TryRead(in wrongTypeSample, out var packet, out var consumed);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
        Assert.AreEqual(0, consumed);
    }

    [TestMethod]
    public void ReturnFalsePacketNullConsumed0GivenEmptySample()
    {
        var actual = Packets.UnsubscribePacket.TryRead(in emptySample, out var packet, out var consumed);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
        Assert.AreEqual(0, consumed);
    }
}