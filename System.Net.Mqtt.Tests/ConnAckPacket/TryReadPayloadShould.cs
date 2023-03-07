using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnAckPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTruePacketNotNullNoExistingSession_GivenValidSample()
    {
        ReadOnlySequence<byte> sample = new(new ReadOnlyMemory<byte>(new byte[] { 0x00, 0x02 }));

        var actual = Packets.ConnAckPacket.TryReadPayload(in sample, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.AreEqual(false, packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullExistingSession_GivenValidSample()
    {
        ReadOnlySequence<byte> sample = new(new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02 }));

        var actual = Packets.ConnAckPacket.TryReadPayload(in sample, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.AreEqual(true, packet.SessionPresent);
    }

    [TestMethod]
    public void ParseOnlyRelevantDataGivenLargerSizeValidSample()
    {
        ReadOnlySequence<byte> largerSizeSample = new(new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x01, 0x00, 0x02, 0x80, 0x00, 0x01, 0x02 }));

        var actual = Packets.ConnAckPacket.TryReadPayload(in largerSizeSample, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.AreEqual(true, packet.SessionPresent);
    }

    [TestMethod]
    public void ParseOnlyRelevantDataGivenLargerSizeFragmentedValidSample()
    {
        var segment1 = new MemorySegment<byte>(new byte[] { 0x01 });
        var segment2 = segment1.Append(new byte[] { 0x02 }).Append(new byte[] { 0x10, 0x20 });
        var largerSizeFragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 1);

        var actual = Packets.ConnAckPacket.TryReadPayload(in largerSizeFragmentedSample, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.AreEqual(true, packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullGivenValidFragmentedSample()
    {
        var segment1 = new MemorySegment<byte>(new byte[] { 0x01 });
        var segment2 = segment1.Append(new byte[] { 0x02 });
        var fragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 1);

        var actual = Packets.ConnAckPacket.TryReadPayload(in fragmentedSample, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.AreEqual(true, packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnFalsePacketNullGivenIncompleteSample()
    {
        ReadOnlySequence<byte> incompleteSample = new(new ReadOnlyMemory<byte>(new byte[] { 0x00 }));

        var actual = Packets.ConnAckPacket.TryReadPayload(in incompleteSample, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalsePacketNullGivenEmptySample()
    {
        var emptySample = ReadOnlySequence<byte>.Empty;

        var actual = Packets.ConnAckPacket.TryReadPayload(in emptySample, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }
}