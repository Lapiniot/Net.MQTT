﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteSequence = System.Buffers.ReadOnlySequence<byte>;

namespace System.Net.Mqtt.Tests.V5.PublishPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_NotDecodePacketId_GivenSampleQoS0()
    {
        var sequence = new ByteSequence(new byte[] {
            0x31, 0x21, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69,
            0x63, 0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x02, 0x01, 0x00, 0x74,
            0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(2), false, 33, out var id, out _, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x00, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS1()
    {
        var sequence = new ByteSequence(new byte[] {
            0x33, 0x23, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63,
            0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa4, 0x02, 0x01, 0x00, 0x74,
            0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(2), true, 35, out var id, out _, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65A4, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodePacketId_GivenSampleQoS2()
    {
        var sequence = new ByteSequence(new byte[] {
            0x35, 0x23, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63,
            0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa5, 0x02, 0x01, 0x00, 0x74,
            0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(2), true, 35, out var id, out _, out _, out _);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65A5, id);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_DefaultProperties_GivenContiguousSample()
    {
        var sequence = new ByteSequence(new byte[] {
            0x35, 0x23, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63,
            0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa5, 0x02, 0x01, 0x00, 0x74,
            0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(2), true, 35, out var id, out var topic, out var payload, out var props);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65a5, id);

        Assert.AreEqual(16, topic.Length);
        Assert.IsTrue(topic.AsSpan().SequenceEqual("testtopic/level1"u8));

        Assert.AreEqual(12, payload.Length);
        Assert.IsTrue(payload.AsSpan().SequenceEqual("test message"u8));

        Assert.AreEqual(0, (byte)props.PayloadFormat);
        Assert.IsTrue(props.ContentType.IsEmpty);
        Assert.IsTrue(props.ResponseTopic.IsEmpty);
        Assert.IsTrue(props.CorrelationData.IsEmpty);
        Assert.IsNull(props.TopicAlias);
        Assert.IsNull(props.SubscriptionIds);
        Assert.IsNull(props.MessageExpiryInterval);
        Assert.IsNull(props.UserProperties);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_DefaultProperties_GivenFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x35, 0x23, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70,
                0x69, 0x63, 0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa5, 0x02, 0x01, 0x00, 0x74 },
            new byte[] {
                0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(2), true, 35, out var id, out var topic, out var payload, out var props);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65a5, id);

        Assert.AreEqual(16, topic.Length);
        Assert.IsTrue(topic.AsSpan().SequenceEqual("testtopic/level1"u8));

        Assert.AreEqual(12, payload.Length);
        Assert.IsTrue(payload.AsSpan().SequenceEqual("test message"u8));

        Assert.AreEqual(0, (byte)props.PayloadFormat);
        Assert.IsTrue(props.ContentType.IsEmpty);
        Assert.IsTrue(props.ResponseTopic.IsEmpty);
        Assert.IsTrue(props.CorrelationData.IsEmpty);
        Assert.IsNull(props.TopicAlias);
        Assert.IsNull(props.SubscriptionIds);
        Assert.IsNull(props.MessageExpiryInterval);
        Assert.IsNull(props.UserProperties);
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenContiguousSample()
    {
        var sequence = new ByteSequence(new byte[] {
            0x35, 0x84, 0x01, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69,
            0x63, 0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa7, 0x6a, 0x01, 0x01,
            0x02, 0x00, 0x00, 0x01, 0x2c, 0x23, 0x00, 0x2a, 0x08, 0x00, 0x0f, 0x72, 0x65,
            0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31,
            0x09, 0x00, 0x15, 0x74, 0x65, 0x73, 0x74, 0x20, 0x63, 0x6f, 0x72, 0x72, 0x65,
            0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x20, 0x64, 0x61, 0x74, 0x61, 0x26, 0x00,
            0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65,
            0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61,
            0x6c, 0x75, 0x65, 0x32, 0x0b, 0xa8, 0x96, 0x10, 0x0b, 0x80, 0x08, 0x0b, 0x2a,
            0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e,
            0x74, 0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(3), true, 139, out var id, out var topic, out var payload, out var props);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65a7, id);

        Assert.AreEqual(16, topic.Length);
        Assert.IsTrue(topic.AsSpan().SequenceEqual("testtopic/level1"u8));

        Assert.AreEqual(12, payload.Length);
        Assert.IsTrue(payload.AsSpan().SequenceEqual("test message"u8));

        Assert.AreEqual(1, (byte)props.PayloadFormat);
        Assert.AreEqual(300u, props.MessageExpiryInterval);
        Assert.AreEqual(3, props.SubscriptionIds.Count);
        Assert.AreEqual(0x40b28u, props.SubscriptionIds[0]);
        Assert.AreEqual(0x400u, props.SubscriptionIds[1]);
        Assert.AreEqual(0x2au, props.SubscriptionIds[2]);
        Assert.AreEqual(42, (ushort)props.TopicAlias);
        Assert.IsTrue(props.ContentType.Span.SequenceEqual("text/plain"u8));
        Assert.IsTrue(props.ResponseTopic.Span.SequenceEqual("response/topic1"u8));
        Assert.IsTrue(props.CorrelationData.Span.SequenceEqual("test correlation data"u8));

        Assert.AreEqual(2, props.UserProperties.Count);
        var (p1, v1) = props.UserProperties[0];
        var (p2, v2) = props.UserProperties[1];
        Assert.IsTrue(p1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(v1.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(p2.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(v2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenContiguousLargeSample()
    {
        var sequence = new ByteSequence(new byte[] {
            0x35, 0x84, 0x01, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69,
            0x63, 0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa7, 0x6a, 0x01, 0x01,
            0x02, 0x00, 0x00, 0x01, 0x2c, 0x23, 0x00, 0x2a, 0x08, 0x00, 0x0f, 0x72, 0x65,
            0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31,
            0x09, 0x00, 0x15, 0x74, 0x65, 0x73, 0x74, 0x20, 0x63, 0x6f, 0x72, 0x72, 0x65,
            0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x20, 0x64, 0x61, 0x74, 0x61, 0x26, 0x00,
            0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65,
            0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61,
            0x6c, 0x75, 0x65, 0x32, 0x0b, 0xa8, 0x96, 0x10, 0x0b, 0x80, 0x08, 0x0b, 0x2a,
            0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e,
            0x74, 0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(3), true, 139, out var id, out var topic, out var payload, out var props);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65a7, id);

        Assert.AreEqual(16, topic.Length);
        Assert.IsTrue(topic.AsSpan().SequenceEqual("testtopic/level1"u8));

        Assert.AreEqual(12, payload.Length);
        Assert.IsTrue(payload.AsSpan().SequenceEqual("test message"u8));

        Assert.AreEqual(1, (byte)props.PayloadFormat);
        Assert.AreEqual(300u, props.MessageExpiryInterval);
        Assert.AreEqual(3, props.SubscriptionIds.Count);
        Assert.AreEqual(0x40b28u, props.SubscriptionIds[0]);
        Assert.AreEqual(0x400u, props.SubscriptionIds[1]);
        Assert.AreEqual(0x2au, props.SubscriptionIds[2]);
        Assert.AreEqual(42, (ushort)props.TopicAlias);
        Assert.IsTrue(props.ContentType.Span.SequenceEqual("text/plain"u8));
        Assert.IsTrue(props.ResponseTopic.Span.SequenceEqual("response/topic1"u8));
        Assert.IsTrue(props.CorrelationData.Span.SequenceEqual("test correlation data"u8));

        Assert.AreEqual(2, props.UserProperties.Count);
        var (p1, v1) = props.UserProperties[0];
        var (p2, v2) = props.UserProperties[1];
        Assert.IsTrue(p1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(v1.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(p2.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(v2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x35, 0x84, 0x01, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69,
                0x63, 0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa7, 0x6a, 0x01, 0x01,
                0x02, 0x00, 0x00, 0x01, 0x2c, 0x23, 0x00, 0x2a, 0x08, 0x00, 0x0f, 0x72, 0x65,
                0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31 },
            new byte[] {
                0x09, 0x00, 0x15, 0x74, 0x65, 0x73, 0x74, 0x20, 0x63, 0x6f, 0x72, 0x72, 0x65,
                0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x20, 0x64, 0x61, 0x74, 0x61, 0x26, 0x00,
                0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65,
                0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61 },
            new byte[] {
                0x6c, 0x75, 0x65, 0x32, 0x0b, 0xa8, 0x96, 0x10, 0x0b, 0x80, 0x08, 0x0b, 0x2a,
                0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e,
                0x74, 0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(3), true, 139, out var id, out var topic, out var payload, out var props);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65a7, id);

        Assert.AreEqual(16, topic.Length);
        Assert.IsTrue(topic.AsSpan().SequenceEqual("testtopic/level1"u8));

        Assert.AreEqual(12, payload.Length);
        Assert.IsTrue(payload.AsSpan().SequenceEqual("test message"u8));

        Assert.AreEqual(1, (byte)props.PayloadFormat);
        Assert.AreEqual(300u, props.MessageExpiryInterval);
        Assert.AreEqual(3, props.SubscriptionIds.Count);
        Assert.AreEqual(0x40b28u, props.SubscriptionIds[0]);
        Assert.AreEqual(0x400u, props.SubscriptionIds[1]);
        Assert.AreEqual(0x2au, props.SubscriptionIds[2]);
        Assert.AreEqual(42, (ushort)props.TopicAlias);
        Assert.IsTrue(props.ContentType.Span.SequenceEqual("text/plain"u8));
        Assert.IsTrue(props.ResponseTopic.Span.SequenceEqual("response/topic1"u8));
        Assert.IsTrue(props.CorrelationData.Span.SequenceEqual("test correlation data"u8));

        Assert.AreEqual(2, props.UserProperties.Count);
        var (p1, v1) = props.UserProperties[0];
        var (p2, v2) = props.UserProperties[1];
        Assert.IsTrue(p1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(v1.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(p2.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(v2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_DecodeTopicAndPayload_GivenFragmentedLargeSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x35, 0x84, 0x01, 0x00, 0x10, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69,
                0x63, 0x2f, 0x6c, 0x65, 0x76, 0x65, 0x6c, 0x31, 0x65, 0xa7, 0x6a, 0x01, 0x01,
                0x02, 0x00, 0x00, 0x01, 0x2c, 0x23, 0x00, 0x2a, 0x08, 0x00, 0x0f, 0x72, 0x65,
                0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x2f, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31 },
            new byte[] {
                0x09, 0x00, 0x15, 0x74, 0x65, 0x73, 0x74, 0x20, 0x63, 0x6f, 0x72, 0x72, 0x65,
                0x6c, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x20, 0x64, 0x61, 0x74, 0x61, 0x26, 0x00,
                0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65,
                0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61 },
            new byte[] {
                0x6c, 0x75, 0x65, 0x32, 0x0b, 0xa8, 0x96, 0x10, 0x0b, 0x80, 0x08, 0x0b, 0x2a,
                0x03, 0x00, 0x0a, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e,
                0x74, 0x65, 0x73, 0x74, 0x20, 0x6d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(3), true, 139, out var id, out var topic, out var payload, out var props);

        Assert.IsTrue(actualResult);

        Assert.AreEqual(0x65a7, id);

        Assert.AreEqual(16, topic.Length);
        Assert.IsTrue(topic.AsSpan().SequenceEqual("testtopic/level1"u8));

        Assert.AreEqual(12, payload.Length);
        Assert.IsTrue(payload.AsSpan().SequenceEqual("test message"u8));

        Assert.AreEqual(1, (byte)props.PayloadFormat);
        Assert.AreEqual(300u, props.MessageExpiryInterval);
        Assert.AreEqual(3, props.SubscriptionIds.Count);
        Assert.AreEqual(0x40b28u, props.SubscriptionIds[0]);
        Assert.AreEqual(0x400u, props.SubscriptionIds[1]);
        Assert.AreEqual(0x2au, props.SubscriptionIds[2]);
        Assert.AreEqual(42, (ushort)props.TopicAlias);
        Assert.IsTrue(props.ContentType.Span.SequenceEqual("text/plain"u8));
        Assert.IsTrue(props.ResponseTopic.Span.SequenceEqual("response/topic1"u8));
        Assert.IsTrue(props.CorrelationData.Span.SequenceEqual("test correlation data"u8));

        Assert.AreEqual(2, props.UserProperties.Count);
        var (p1, v1) = props.UserProperties[0];
        var (p2, v2) = props.UserProperties[1];
        Assert.IsTrue(p1.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(v1.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(p2.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(v2.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenContiguousIncompleteSample()
    {
        var sequence = new ByteSequence(new byte[] { 0b111011, 14, 0x00, 0x05, 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out var id, out var topic, out var payload, out _);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.AreEqual(null, topic);
        Assert.AreEqual(null, payload);
    }

    [TestMethod]
    public void ReturnFalseAndParamsUninitialized_GivenFragmentedIncompleteSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] { 0b111011, 14, 0x00, 0x05 },
            new byte[] { 0x61, 0x2f, 0x62, 0x2f, 0x63, 0x00, 0x04, 0x03, 0x04 });

        var actualResult = Packets.V5.PublishPacket.TryReadPayload(sequence.Slice(2), true, 14, out var id, out var topic, out var payload, out _);

        Assert.IsFalse(actualResult);
        Assert.AreEqual(0, id);
        Assert.AreEqual(null, topic);
        Assert.AreEqual(null, payload);
    }
}