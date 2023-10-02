using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.SubscribePacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidSample()
    {
        var sequence = new ReadOnlySequence<byte>([0x82, 0x49, 0xfa, 0x98, 0x22, 0x0b, 0x02, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x00, 0x0b, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x2f, 0x23, 0x1d, 0x00, 0x0c, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x2f, 0x6c, 0x31, 0x1d, 0x00, 0x04, 0x2b, 0x2f, 0x6c, 0x31, 0x1d]);

        var actual = Packets.V5.SubscribePacket.TryReadPayload(sequence.Slice(2), 73, out var id, out var subscriptionId, out var props, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0xFA98, id);
        Assert.AreEqual(0x02u, subscriptionId);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Filter.AsSpan().SequenceEqual("testtopic/#"u8));
        Assert.AreEqual(0x1d, filters[0].Options);
        Assert.IsTrue(filters[1].Filter.AsSpan().SequenceEqual("testtopic/l1"u8));
        Assert.AreEqual(0x1d, filters[1].Options);
        Assert.IsTrue(filters[2].Filter.AsSpan().SequenceEqual("+/l1"u8));
        Assert.AreEqual(0x1d, filters[2].Options);
        Assert.AreEqual(2, props.Count);
        Assert.IsTrue(props[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(props[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(props[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(props[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x82, 0x49, 0xfa, 0x98, 0x22, 0x0b, 0x02, 0x26, 0x00, 0x05, 0x70, 0x72,
                0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, },
            new byte[] {
                0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c,
                0x75, 0x65, 0x32, 0x00, 0x0b, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70 },
            new byte[] {
                0x69, 0x63, 0x2f, 0x23, 0x1d, 0x00, 0x0c, 0x74, 0x65, 0x73, 0x74, 0x74,
                0x6f, 0x70, 0x69, 0x63, 0x2f, 0x6c, 0x31, 0x1d, 0x00, 0x04, 0x2b, 0x2f },
            new byte[] { 0x6c, 0x31, 0x1d });

        var actual = Packets.V5.SubscribePacket.TryReadPayload(sequence.Slice(2), 73, out var id, out var subscriptionId, out var props, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0xFA98, id);
        Assert.AreEqual(0x02u, subscriptionId);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Filter.AsSpan().SequenceEqual("testtopic/#"u8));
        Assert.AreEqual(0x1d, filters[0].Options);
        Assert.IsTrue(filters[1].Filter.AsSpan().SequenceEqual("testtopic/l1"u8));
        Assert.AreEqual(0x1d, filters[1].Options);
        Assert.IsTrue(filters[2].Filter.AsSpan().SequenceEqual("+/l1"u8));
        Assert.AreEqual(0x1d, filters[2].Options);
        Assert.AreEqual(2, props.Count);
        Assert.IsTrue(props[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(props[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(props[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(props[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidLargerBufferSample()
    {
        var sequence = new ReadOnlySequence<byte>([0x82, 0x49, 0xfa, 0x98, 0x22, 0x0b, 0x02, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x00, 0x0b, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x2f, 0x23, 0x1d, 0x00, 0x0c, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x2f, 0x6c, 0x31, 0x1d, 0x00, 0x04, 0x2b, 0x2f, 0x6c, 0x31, 0x1d, 0x00, 0x00, 0x00]);

        var actual = Packets.V5.SubscribePacket.TryReadPayload(sequence.Slice(2), 73, out var id, out var subscriptionId, out var props, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0xFA98, id);
        Assert.AreEqual(0x02u, subscriptionId);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Filter.AsSpan().SequenceEqual("testtopic/#"u8));
        Assert.AreEqual(0x1d, filters[0].Options);
        Assert.IsTrue(filters[1].Filter.AsSpan().SequenceEqual("testtopic/l1"u8));
        Assert.AreEqual(0x1d, filters[1].Options);
        Assert.IsTrue(filters[2].Filter.AsSpan().SequenceEqual("+/l1"u8));
        Assert.AreEqual(0x1d, filters[2].Options);
        Assert.AreEqual(2, props.Count);
        Assert.IsTrue(props[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(props[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(props[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(props[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_IdAndFiltersOutParams_GivenValidLargerBufferFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x82, 0x49, 0xfa, 0x98, 0x22, 0x0b, 0x02, 0x26, 0x00, 0x05, 0x70, 0x72,
                0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, },
            new byte[] {
                0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c,
                0x75, 0x65, 0x32, 0x00, 0x0b, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70 },
            new byte[] {
                0x69, 0x63, 0x2f, 0x23, 0x1d, 0x00, 0x0c, 0x74, 0x65, 0x73, 0x74, 0x74,
                0x6f, 0x70, 0x69, 0x63, 0x2f, 0x6c, 0x31, 0x1d, 0x00, 0x04, 0x2b, 0x2f },
            new byte[] { 0x6c, 0x31, 0x1d, 0x00, 0x00, 0x00 });

        var actual = Packets.V5.SubscribePacket.TryReadPayload(sequence.Slice(2), 73, out var id, out var subscriptionId, out var props, out var filters);

        Assert.IsTrue(actual);
        Assert.AreEqual(0xFA98, id);
        Assert.AreEqual(0x02u, subscriptionId);
        Assert.IsNotNull(filters);
        Assert.AreEqual(3, filters.Count);
        Assert.IsTrue(filters[0].Filter.AsSpan().SequenceEqual("testtopic/#"u8));
        Assert.AreEqual(0x1d, filters[0].Options);
        Assert.IsTrue(filters[1].Filter.AsSpan().SequenceEqual("testtopic/l1"u8));
        Assert.AreEqual(0x1d, filters[1].Options);
        Assert.IsTrue(filters[2].Filter.AsSpan().SequenceEqual("+/l1"u8));
        Assert.AreEqual(0x1d, filters[2].Options);
        Assert.AreEqual(2, props.Count);
        Assert.IsTrue(props[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(props[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(props[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(props[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenIncompleteSample()
    {
        var sequence = new ReadOnlySequence<byte>([0x82, 0x49, 0xfa, 0x98, 0x22, 0x0b, 0x02, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c]);

        var actual = Packets.V5.SubscribePacket.TryReadPayload(sequence.Slice(2), 73, out var id, out var subscriptionId, out var props, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.AreEqual(0u, subscriptionId);
        Assert.IsNull(filters);
        Assert.IsNull(props);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenIncompleteFragmentedSample()
    {
        var sequence = SequenceFactory.Create<byte>(
            new byte[] {
                0x82, 0x49, 0xfa, 0x98, 0x22, 0x0b, 0x02, 0x26, 0x00, 0x05, 0x70, 0x72,
                0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, },
            new byte[] {
                0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c,
                0x75, 0x65, 0x32, 0x00, 0x0b, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70 });

        var actual = Packets.V5.SubscribePacket.TryReadPayload(sequence.Slice(2), 73, out var id, out var subscriptionId, out var props, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.AreEqual(0u, subscriptionId);
        Assert.IsNull(filters);
        Assert.IsNull(props);
    }

    [TestMethod]
    public void ReturnFalse_IdAndFiltersUnitialized_GivenEmptyBufferSample()
    {
        var actual = Packets.V5.SubscribePacket.TryReadPayload(ReadOnlySequence<byte>.Empty, 26, out var id, out var subscriptionId, out var props, out var filters);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, id);
        Assert.AreEqual(0u, subscriptionId);
        Assert.IsNull(filters);
        Assert.IsNull(props);
    }
}