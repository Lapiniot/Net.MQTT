using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubAckPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_PacketNotNull_GivenValidSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] { 0x00, 0x02, 0x01, 0x00, 0x02, 0x80 });

        var actual = Packets.V3.SubAckPacket.TryReadPayload(in sequence, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ParseOnlyRelevantData_GivenLargerSizeValidSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] { 0x00, 0x02, 0x01, 0x00, 0x02, 0x80, 0x00, 0x01, 0x02 });

        var actual = Packets.V3.SubAckPacket.TryReadPayload(in sequence, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ParseOnlyRelevantData_GivenLargerSizeFragmentedValidSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0x00, 0x02 },
            new byte[] { 0x01, 0x00 },
            new byte[] { 0x02, 0x80 },
            new byte[] { 0x00, 0x01, 0x02 });

        var actual = Packets.V3.SubAckPacket.TryReadPayload(in sequence, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ReturnTrue_PacketNotNull_GivenValidFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x02 }, new byte[] { 0x01, 0x00 }, new byte[] { 0x02, 0x80 });

        var actual = Packets.V3.SubAckPacket.TryReadPayload(in sequence, 6, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(4, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
        Assert.AreEqual(0x80, packet.Feedback.Span[3]);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteSample()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[] { 0x00, 0x02, 0x01 });

        var actual = Packets.V3.SubAckPacket.TryReadPayload(in sequence, 6, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenEmptySample()
    {
        var actual = Packets.V3.SubAckPacket.TryReadPayload(ReadOnlySequence<byte>.Empty, 6, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }
}