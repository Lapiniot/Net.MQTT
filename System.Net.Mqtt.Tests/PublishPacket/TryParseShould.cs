﻿using System.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteSequence = System.Buffers.ReadOnlySequence<byte>;

namespace System.Net.Mqtt.Tests.PublishPacket;

[TestClass]
public class TryParseShould
{
    private readonly ByteSequence sampleComplete = new ByteSequence(
        new byte[]
        {
                0x3b, 0x0e, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04, 0x03,
                0x04, 0x05, 0x04, 0x03
        });

    private readonly ByteSequence sampleDuplicateFlag = new ByteSequence(
        new byte[]
        {
                0x38, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63
        });


    private readonly ByteSequence sampleFragmented;

    private readonly ByteSequence sampleIncomplete = new ByteSequence(
        new byte[]
        {
                0x3b, 0x0e, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04, 0x03
        });

    private readonly ByteSequence sampleNoFlags = new ByteSequence(
        new byte[]
        {
                0x30, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63
        });

    private readonly ByteSequence sampleQosAtLeastOnce = new ByteSequence(
        new byte[]
        {
                0x32, 0x09, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04
        });

    private readonly ByteSequence sampleQosAtMostOnce = new ByteSequence(
        new byte[]
        {
                0x30, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04
        });

    private readonly ByteSequence sampleQosExactlyOnce = new ByteSequence(
        new byte[]
        {
                0x34, 0x09, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04
        });

    private readonly ByteSequence sampleRetainFlag = new ByteSequence(
        new byte[]
        {
                0x31, 0x07, 0x00, 0x05,
                0x61, 0x2f, 0x62, 0x2f,
                0x63
        });

    public TryParseShould()
    {
        var segment1 = new Segment<byte>(new byte[]
        {
                0x3b, 0x0e, 0x00, 0x05
        });

        var segment2 = segment1.Append(new byte[]
        {
                0x61, 0x2f, 0x62, 0x2f,
                0x63, 0x00, 0x04, 0x03,
                0x04, 0x05, 0x04, 0x03
        });

        sampleFragmented = new ByteSequence(segment1, 0, segment2, 12);
    }

    [TestMethod]
    public void ReturnQoSLevelAtMostOnceGivenSampleWithQoS0()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleQosAtMostOnce, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0, p.QoSLevel);
    }

    [TestMethod]
    public void ReturnQoSLevelAtLeastOnceGivenSampleWithQoS1()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleQosAtLeastOnce, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(1, p.QoSLevel);
    }

    [TestMethod]
    public void ReturnQoSLevelExactlyOnceGivenSampleWithQoS2()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleQosExactlyOnce, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(2, p.QoSLevel);
    }

    [TestMethod]
    public void ReturnDuplicateTrueGivenSampleWithDupFlag1()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleDuplicateFlag, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(p.Duplicate);
    }

    [TestMethod]
    public void ReturnDuplicateFalseGivenSampleWithDupFlag0()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleNoFlags, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.IsFalse(p.Duplicate);
    }

    [TestMethod]
    public void ReturnRetainTrueGivenSampleWithRetainFlag1()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleRetainFlag, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.IsTrue(p.Retain);
    }

    [TestMethod]
    public void ReturnRetainFalseGivenSampleWithRetainFlag0()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleNoFlags, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.IsFalse(p.Retain);
    }

    [TestMethod]
    public void ReturnTruePacketNotNullConsumed16GivenValidSample()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleComplete, out var packet, out var consumed);

        Assert.IsTrue(actualResult);
        Assert.IsNotNull(packet);
        Assert.AreEqual(16, consumed);
    }

    [TestMethod]
    public void ReturnFalsePacketNullConsumed0GivenSampleIncomplete()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleIncomplete, out var packet, out var consumed);

        Assert.IsFalse(actualResult);
        Assert.IsNull(packet);
        Assert.AreEqual(0, consumed);
    }

    [TestMethod]
    public void NotDecodePacketIdGivenSampleQoS0()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleQosAtMostOnce, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x00, p.Id);
    }

    [TestMethod]
    public void DecodePacketId0x04GivenSampleQoS1()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleQosAtLeastOnce, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x04, p.Id);
    }

    [TestMethod]
    public void DecodePacketId0x04GivenSampleQoS2()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleQosExactlyOnce, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x04, p.Id);
    }

    [TestMethod]
    public void DecodeTopicabcGivenSample()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleComplete, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual("a/b/c", p.Topic);
    }

    [TestMethod]
    public void DecodeTopicabcGivenSampleFragmented()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleFragmented, out var p, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual("a/b/c", p.Topic);
    }

    [TestMethod]
    public void DecodePayload0x030x040x050x040x03GivenSample()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleComplete, out var p, out _);

        Assert.IsTrue(actualResult);

        var span = p.Payload.Span;

        Assert.AreEqual(5, span.Length);

        Assert.AreEqual(0x03, span[0]);
        Assert.AreEqual(0x04, span[1]);
        Assert.AreEqual(0x05, span[2]);
        Assert.AreEqual(0x04, span[3]);
        Assert.AreEqual(0x03, span[4]);
    }

    [TestMethod]
    public void DecodePayload0x030x040x050x040x03GivenSampleFragmented()
    {
        var actualResult = Packets.PublishPacket.TryRead(sampleFragmented, out var p, out _);

        Assert.IsTrue(actualResult);

        var span = p.Payload.Span;

        Assert.AreEqual(5, span.Length);

        Assert.AreEqual(0x03, span[0]);
        Assert.AreEqual(0x04, span[1]);
        Assert.AreEqual(0x05, span[2]);
        Assert.AreEqual(0x04, span[3]);
        Assert.AreEqual(0x03, span[4]);
    }
}