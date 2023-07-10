using System.Net.Mqtt.Packets.V5;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteSequence = System.Buffers.ReadOnlySequence<byte>;

namespace System.Net.Mqtt.Tests.V5.PublishResponsePacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_PacketId_DefaultReasonCode_GivenContiguousSample2Bytes()
    {
        var sequence = new ByteSequence(new byte[] { 0x31, 0x21 });

        var actual = Packets.V5.PublishResponsePacket.TryReadPayload(sequence, out var packetId, out var reasonCode);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x3121, packetId);
        Assert.AreEqual(ReasonCode.Success, reasonCode);
    }

    [TestMethod]
    public void ReturnTrue_PacketId_ReasonCode_GivenContiguousSample3Bytes()
    {
        var sequence = new ByteSequence(new byte[] { 0x31, 0x21, 0x80 });

        var actual = Packets.V5.PublishResponsePacket.TryReadPayload(sequence, out var packetId, out var reasonCode);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x3121, packetId);
        Assert.AreEqual(ReasonCode.UnspecifiedError, reasonCode);
    }

    [TestMethod]
    public void ReturnTrue_PacketId_DefaultReasonCode_GivenFragmentedSample2Bytes()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 0x31 }, new byte[] { 0x21 });

        var actual = Packets.V5.PublishResponsePacket.TryReadPayload(sequence, out var packetId, out var reasonCode);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x3121, packetId);
        Assert.AreEqual(ReasonCode.Success, reasonCode);
    }

    [TestMethod]
    public void ReturnTrue_PacketId_ReasonCode_GivenFragmentedSample3Bytes()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 0x31 }, new byte[] { 0x21 }, new byte[] { 0x80 });

        var actual = Packets.V5.PublishResponsePacket.TryReadPayload(sequence, out var packetId, out var reasonCode);

        Assert.IsTrue(actual);
        Assert.AreEqual(0x3121, packetId);
        Assert.AreEqual(ReasonCode.UnspecifiedError, reasonCode);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteSample()
    {
        var sequence = new ByteSequence(new byte[] { 0x31 });

        var actual = Packets.V5.PublishResponsePacket.TryReadPayload(sequence, out var _, out var _);

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalse_GivenIncompleteFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(new byte[] { 0x31 }, Array.Empty<byte>());

        var actual = Packets.V5.PublishResponsePacket.TryReadPayload(sequence, out var _, out var _);

        Assert.IsFalse(actual);
    }
}