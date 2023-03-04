using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubAckPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTruePacketNotNullGivenValidSample()
    {
        ReadOnlySequence<byte> sample = new(new ReadOnlyMemory<byte>(new byte[] { 0x00, 0x02, 0x01, 0x00, 0x02, 0x80 }));

        var actual = Packets.SubAckPacket.TryReadPayload(in sample, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ParseOnlyRelevantDataGivenLargerSizeValidSample()
    {
        ReadOnlySequence<byte> largerSizeSample = new(new ReadOnlyMemory<byte>(new byte[] { 0x00, 0x02, 0x01, 0x00, 0x02, 0x80, 0x00, 0x01, 0x02 }));

        var actual = Packets.SubAckPacket.TryReadPayload(in largerSizeSample, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ParseOnlyRelevantDataGivenLargerSizeFragmentedValidSample()
    {
        var segment1 = new Segment<byte>(new byte[] { 0x00, 0x02 });
        var segment2 = segment1.Append(new byte[] { 0x01, 0x00 }).Append(new byte[] { 0x02, 0x80 });
        var segment3 = segment2.Append(new byte[] { 0x00, 0x01, 0x02 });
        var largerSizeFragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment3, 3);

        var actual = Packets.SubAckPacket.TryReadPayload(in largerSizeFragmentedSample, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullGivenValidFragmentedSample()
    {
        var segment1 = new Segment<byte>(new byte[] { 0x00 });
        var segment2 = segment1.Append(new byte[] { 0x02 }).Append(new byte[] { 0x01, 0x00 }).Append(new byte[] { 0x02, 0x80 });
        var fragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 2);

        var actual = Packets.SubAckPacket.TryReadPayload(in fragmentedSample, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ReturnFalsePacketNullGivenIncompleteSample()
    {
        ReadOnlySequence<byte> incompleteSample = new(new ReadOnlyMemory<byte>(new byte[] { 0x00, 0x02, 0x01 }));

        var actual = Packets.SubAckPacket.TryReadPayload(in incompleteSample, 6, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalsePacketNullGivenEmptySample()
    {
        var emptySample = ReadOnlySequence<byte>.Empty;

        var actual = Packets.SubAckPacket.TryReadPayload(in emptySample, 6, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }
}