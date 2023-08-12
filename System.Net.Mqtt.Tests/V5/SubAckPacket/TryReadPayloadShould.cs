using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.SubAckPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_PacketNotNull_GivenValidSample()
    {
        var sequence = new ReadOnlySequence<byte>([0x90, 0x33, 0x00, 0x02, 0x2d, 0x1f, 0x00, 0x0a, 0x61, 0x6e, 0x79, 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x01, 0x00, 0x02]);

        var actual = Packets.V5.SubAckPacket.TryReadPayload(sequence.Slice(2), 51, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.Id);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("any reason"u8));
        Assert.AreEqual(2, packet.Properties.Count);
        Assert.IsTrue(packet.Properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.Properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.Properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.Properties[1].Item2.Span.SequenceEqual("value2"u8));
        Assert.AreEqual(3, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
    }

    [TestMethod]
    public void ParseOnlyRelevantData_GivenLargerSizeValidSample()
    {
        var sequence = new ReadOnlySequence<byte>([0x90, 0x33, 0x00, 0x02, 0x2d, 0x1f, 0x00, 0x0a, 0x61, 0x6e, 0x79, 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x01, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

        var actual = Packets.V5.SubAckPacket.TryReadPayload(sequence.Slice(2), 51, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.Id);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("any reason"u8));
        Assert.AreEqual(2, packet.Properties.Count);
        Assert.IsTrue(packet.Properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.Properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.Properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.Properties[1].Item2.Span.SequenceEqual("value2"u8));
        Assert.AreEqual(3, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
    }

    [TestMethod]
    public void ReturnTrue_PacketNotNull_GivenValidFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0x90, 0x33, 0x00, 0x02, 0x2d, 0x1f, 0x00, 0x0a, 0x61, 0x6e, 0x79 },
            new byte[] { 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x26, 0x00, 0x05, 0x70 },
            new byte[] { 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65 },
            new byte[] { 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06 },
            new byte[] { 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x01, 0x00, 0x02 }
        );

        var actual = Packets.V5.SubAckPacket.TryReadPayload(sequence.Slice(2), 51, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.Id);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("any reason"u8));
        Assert.AreEqual(2, packet.Properties.Count);
        Assert.IsTrue(packet.Properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.Properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.Properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.Properties[1].Item2.Span.SequenceEqual("value2"u8));
        Assert.AreEqual(3, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
    }

    [TestMethod]
    public void ParseOnlyRelevantData_GivenLargerSizeFragmentedValidSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0x90, 0x33, 0x00, 0x02, 0x2d, 0x1f, 0x00, 0x0a, 0x61, 0x6e, 0x79 },
            new byte[] { 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x26, 0x00, 0x05, 0x70 },
            new byte[] { 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65 },
            new byte[] { 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06 },
            new byte[] { 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x01, 0x00, 0x02, 0x00, 0x00 }
        );

        var actual = Packets.V5.SubAckPacket.TryReadPayload(sequence.Slice(2), 51, out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.Id);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("any reason"u8));
        Assert.AreEqual(2, packet.Properties.Count);
        Assert.IsTrue(packet.Properties[0].Item1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.Properties[0].Item2.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.Properties[1].Item1.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.Properties[1].Item2.Span.SequenceEqual("value2"u8));
        Assert.AreEqual(3, packet.Feedback.Length);
        Assert.AreEqual(1, packet.Feedback.Span[0]);
        Assert.AreEqual(0, packet.Feedback.Span[1]);
        Assert.AreEqual(2, packet.Feedback.Span[2]);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteSample()
    {
        var sequence = new ReadOnlySequence<byte>([0x90, 0x33, 0x00, 0x02, 0x2d, 0x1f, 0x00, 0x0a, 0x61, 0x6e, 0x79, 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65]);

        var actual = Packets.V5.SubAckPacket.TryReadPayload(sequence.Slice(2), 51, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenEmptySample()
    {
        var actual = Packets.V5.SubAckPacket.TryReadPayload(ReadOnlySequence<byte>.Empty, 51, out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }
}