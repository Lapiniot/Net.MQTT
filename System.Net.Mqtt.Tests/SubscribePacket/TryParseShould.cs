using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubscribePacket;

[TestClass]
public class TryParseShould
{
    private readonly ReadOnlySequence<byte> emptySample = ReadOnlySequence<byte>.Empty;
    private readonly ReadOnlySequence<byte> fragmentedSample;

    private readonly ReadOnlySequence<byte> incompleteSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f
    }));

    private readonly ReadOnlySequence<byte> largerBufferSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
        0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
        0x68, 0x2f, 0x69, 0x00, 0x00, 0x05, 0x67, 0x2f,
        0x68, 0x2f, 0x69, 0x00
    }));

    private readonly ReadOnlySequence<byte> largerFragmentedSample;

    private readonly ReadOnlySequence<byte> sample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
        0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
        0x68, 0x2f, 0x69, 0x00
    }));

    private readonly ReadOnlySequence<byte> wrongTypeSample = new(new ReadOnlyMemory<byte>(new byte[]
    {
        0x60, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
        0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
        0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
        0x68, 0x2f, 0x69, 0x00
    }));

    public TryParseShould()
    {
        var segment1 = new Segment<byte>(new byte[] { 0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f });

        var segment2 = segment1
            .Append(new byte[] { 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f })
            .Append(new byte[] { 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f })
            .Append(new byte[] { 0x68, 0x2f, 0x69, 0x00 });

        var segment3 = segment2.Append(new byte[] { 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x00 });

        fragmentedSample = new(segment1, 0, segment2, 4);
        largerFragmentedSample = new(segment1, 0, segment3, 8);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullGivenValidSample()
    {
        var actual = Packets.SubscribePacket.TryRead(in sample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        var topics = packet.Filters;
        Assert.AreEqual(sample.Length, consumed);
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0].Filter);
        Assert.AreEqual(2, topics[0].QoS);
        Assert.AreEqual("d/e/f", topics[1].Filter);
        Assert.AreEqual(1, topics[1].QoS);
        Assert.AreEqual("g/h/i", topics[2].Filter);
        Assert.AreEqual(0, topics[2].QoS);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullGivenValidFragmentedSample()
    {
        var actual = Packets.SubscribePacket.TryRead(in fragmentedSample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(sample.Length, consumed);
        var topics = packet.Filters;
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0].Filter);
        Assert.AreEqual(2, topics[0].QoS);
        Assert.AreEqual("d/e/f", topics[1].Filter);
        Assert.AreEqual(1, topics[1].QoS);
        Assert.AreEqual("g/h/i", topics[2].Filter);
        Assert.AreEqual(0, topics[2].QoS);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullConsumed28GivenLargerBufferSample()
    {
        var actual = Packets.SubscribePacket.TryRead(in largerBufferSample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(28, consumed);
        var topics = packet.Filters;
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0].Filter);
        Assert.AreEqual(2, topics[0].QoS);
        Assert.AreEqual("d/e/f", topics[1].Filter);
        Assert.AreEqual(1, topics[1].QoS);
        Assert.AreEqual("g/h/i", topics[2].Filter);
        Assert.AreEqual(0, topics[2].QoS);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullConsumed28GivenLargerFragmentedBufferSample()
    {
        var actual = Packets.SubscribePacket.TryRead(in largerFragmentedSample, out var packet, out var consumed);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(28, consumed);
        var topics = packet.Filters;
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("a/b/c", topics[0].Filter);
        Assert.AreEqual(2, topics[0].QoS);
        Assert.AreEqual("d/e/f", topics[1].Filter);
        Assert.AreEqual(1, topics[1].QoS);
        Assert.AreEqual("g/h/i", topics[2].Filter);
        Assert.AreEqual(0, topics[2].QoS);
    }

    [TestMethod]
    public void ReturnFalsePacketNullConsumed0GivenIncompleteSample()
    {
        var actual = Packets.SubscribePacket.TryRead(in incompleteSample, out var packet, out var consumed);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
        Assert.AreEqual(0, consumed);
    }

    [TestMethod]
    public void ReturnFalsePacketNullConsumed0GivenWrongTypeSample()
    {
        var actual = Packets.SubscribePacket.TryRead(in wrongTypeSample, out var packet, out var consumed);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
        Assert.AreEqual(0, consumed);
    }

    [TestMethod]
    public void ReturnFalsePacketNullGivenEmptySample()
    {
        var actual = Packets.SubscribePacket.TryRead(in emptySample, out var packet, out var consumed);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
        Assert.AreEqual(0, consumed);
    }
}