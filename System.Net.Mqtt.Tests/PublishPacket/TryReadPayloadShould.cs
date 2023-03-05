using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteSequence = System.Buffers.ReadOnlySequence<byte>;

namespace System.Net.Mqtt.Tests.PublishPacket;

[TestClass]
public class TryReadPayloadShould
{
    private readonly ByteSequence sampleСontinuous = new(new byte[]
    {
        0b111011, 14, 0x00, 0x05,
        0x61, 0x2f, 0x62, 0x2f,
        0x63, 0x00, 0x04, 0x03,
        0x04, 0x05, 0x04, 0x03
    });
    private readonly ByteSequence sampleСontinuousIncomplete = new(new byte[]
    {
        0b111011, 14, 0x00, 0x05,
        0x61, 0x2f, 0x62, 0x2f,
        0x63, 0x00, 0x04, 0x03
    });
    private readonly ByteSequence sampleFragmented;
    private readonly ByteSequence sampleFragmentedIncomplete;
    private readonly ByteSequence sampleQosAtLeastOnce = new(new byte[]
    {
        0b110010, 9, 0x00, 0x05,
        0x61, 0x2f, 0x62, 0x2f,
        0x63, 0x00, 0x04
    });

    private readonly ByteSequence sampleQosAtMostOnce = new(new byte[]
    {
        0b110000, 7, 0x00, 0x05,
        0x61, 0x2f, 0x62, 0x2f,
        0x63, 0x00, 0x04
    });

    private readonly ByteSequence sampleQosExactlyOnce = new(new byte[]
    {
        0b110100, 9, 0x00, 0x05,
        0x61, 0x2f, 0x62, 0x2f,
        0x63, 0x00, 0x04
    });

    public TryReadPayloadShould()
    {
        var segment1 = new Segment<byte>(new byte[] { 0b111011, 14, 0x00, 0x05 });

        sampleFragmented = new(segment1, 0, segment1.Append(new byte[]
        {
            0x61, 0x2f, 0x62, 0x2f,
            0x63, 0x00, 0x04, 0x03,
            0x04, 0x05, 0x04, 0x03
        }), 12);

        sampleFragmentedIncomplete = sampleFragmented.Slice(0, sampleFragmented.Length - 4);
    }

    [TestMethod]
    public void ReturnTrue_NotDecodePacketId_GivenSampleQoS0()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleQosAtMostOnce.Slice(2), 0b110000, 7, out var id, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x00, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS1()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleQosAtLeastOnce.Slice(2), 0b110010, 9, out var id, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x04, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS2()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleQosExactlyOnce.Slice(2), 0b110100, 9, out var id, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x04, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopic_GivenСontinuousSample()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleСontinuous.Slice(2), 0b111011, 14, out _, out var topic, out _);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(topic.AsSpan().SequenceEqual("a/b/c"u8));
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopic_GivenFragmentedSample()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleFragmented.Slice(2), 0b111011, 14, out _, out var topic, out _);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(topic.AsSpan().SequenceEqual("a/b/c"u8));
    }

    [TestMethod]
    public void ReturnTrue_DecodePayload_GivenСontinuousSample()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleСontinuous.Slice(2), 0b111011, 14, out _, out _, out var payload);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(5, payload.Length);

        Assert.AreEqual(0x03, payload[0]);
        Assert.AreEqual(0x04, payload[1]);
        Assert.AreEqual(0x05, payload[2]);
        Assert.AreEqual(0x04, payload[3]);
        Assert.AreEqual(0x03, payload[4]);
    }

    [TestMethod]
    public void ReturnTrue_DecodePayload_GivenFragmentedSample()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleFragmented.Slice(2), 0b111011, 14, out _, out _, out var payload);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(5, payload.Length);

        Assert.AreEqual(0x03, payload[0]);
        Assert.AreEqual(0x04, payload[1]);
        Assert.AreEqual(0x05, payload[2]);
        Assert.AreEqual(0x04, payload[3]);
        Assert.AreEqual(0x03, payload[4]);
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenСontinuousIncompleteSample()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleСontinuousIncomplete.Slice(2), 0b111011, 14, out var id, out var topic, out var payload);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.AreEqual(null, topic);
        Assert.AreEqual(null, payload);
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenFragmentedIncompleteSample()
    {
        var actualResult = Packets.PublishPacket.TryReadPayload(sampleFragmentedIncomplete.Slice(2), 0b111011, 14, out var id, out var topic, out var payload);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.AreEqual(null, topic);
        Assert.AreEqual(null, payload);
    }
}