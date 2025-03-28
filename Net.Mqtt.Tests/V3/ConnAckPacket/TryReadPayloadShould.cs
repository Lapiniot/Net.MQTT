﻿namespace Net.Mqtt.Tests.V3.ConnAckPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTruePacketNotNullNoExistingSession_GivenValidSample()
    {
        ReadOnlySequence<byte> sequence = new([0x00, 0x02]);

        var actual = Packets.V3.ConnAckPacket.TryReadPayload(in sequence, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullExistingSession_GivenValidSample()
    {
        ReadOnlySequence<byte> sequence = new([0x01, 0x02]);

        var actual = Packets.V3.ConnAckPacket.TryReadPayload(in sequence, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsTrue(packet.SessionPresent);
    }

    [TestMethod]
    public void ParseOnlyRelevantDataGivenLargerSizeValidSample()
    {
        ReadOnlySequence<byte> sequence = new([0x01, 0x02, 0x01, 0x00, 0x02, 0x80, 0x00, 0x01, 0x02]);

        var actual = Packets.V3.ConnAckPacket.TryReadPayload(in sequence, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsTrue(packet.SessionPresent);
    }

    [TestMethod]
    public void ParseOnlyRelevantDataGivenLargerSizeFragmentedValidSample()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 0x01 }, new byte[] { 0x02 }, new byte[] { 0x10, 0x20 });

        var actual = Packets.V3.ConnAckPacket.TryReadPayload(in sequence, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsTrue(packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullGivenValidFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 0x01 }, new byte[] { 0x02 });

        var actual = Packets.V3.ConnAckPacket.TryReadPayload(in sequence, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsTrue(packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnFalsePacketNullGivenIncompleteSample()
    {
        ReadOnlySequence<byte> sequence = new([0x00]);

        var actual = Packets.V3.ConnAckPacket.TryReadPayload(in sequence, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalsePacketNullGivenEmptySample()
    {
        var sequence = ReadOnlySequence<byte>.Empty;

        var actual = Packets.V3.ConnAckPacket.TryReadPayload(in sequence, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }
}