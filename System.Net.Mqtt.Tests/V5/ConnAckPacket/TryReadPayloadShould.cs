using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.ConnAckPacket;

[TestClass]
public class TryReadPayloadShould
{
    [TestMethod]
    public void ReturnTrue_ParseStatusCodeAndSessionPresent_GivenContinuousSequence()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x00, 0x02, 0x00]), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);

        actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x01, 0x04, 0x00]), out packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x04, packet.StatusCode);
        Assert.IsTrue(packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTrue_ParseStatusCodeAndSessionPresent_GivenLargerContinuousSequence()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x00, 0x02, 0x00, 0x00, 0x01]), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTrue_SetDefaultPropValues_GivenContinuousSequenceWithNoProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x00, 0x02, 0x00]), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
        Assert.AreEqual(null, packet.AssignedClientId);
        Assert.AreEqual(null, packet.AuthData);
        Assert.AreEqual(null, packet.AuthMethod);
        Assert.AreEqual(null, packet.MaximumPacketSize);
        Assert.AreEqual(QoSLevel.QoS2, packet.MaximumQoS);
        Assert.AreEqual(null, packet.ReasonString);
        Assert.AreEqual(ushort.MaxValue, packet.ReceiveMaximum);
        Assert.AreEqual(null, packet.ResponseInfo);
        Assert.AreEqual(true, packet.RetainAvailable);
        Assert.AreEqual(null, packet.ServerKeepAlive);
        Assert.AreEqual(null, packet.ServerReference);
        Assert.AreEqual(null, packet.SessionExpiryInterval);
        Assert.AreEqual(true, packet.SharedSubscriptionAvailable);
        Assert.AreEqual(true, packet.SubscriptionIdentifiersAvailable);
        Assert.AreEqual(true, packet.WildcardSubscriptionAvailable);
        Assert.AreEqual(0, packet.TopicAliasMaximum);
        Assert.IsNull(packet.UserProperties);
    }

    [TestMethod]
    public void ReturnTrue_ParseProps_GivenContinuousSequenceWithProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x00, 0x02, 0x91, 0x01, 0x11, 0x00, 0x00, 0x0e, 0x10, 0x21, 0x04, 0x00, 0x24, 0x01, 0x25, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x12, 0x00, 0x0d, 0x30, 0x48, 0x4d, 0x55, 0x50, 0x43, 0x4f, 0x42, 0x50, 0x4c, 0x4e, 0x32, 0x4b, 0x22, 0x01, 0x00, 0x1f, 0x00, 0x0b, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x28, 0x00, 0x29, 0x00, 0x2a, 0x00, 0x13, 0x00, 0xb4, 0x1a, 0x00, 0x0d, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74, 0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x15, 0x00, 0x06, 0x62, 0x65, 0x61, 0x72, 0x65, 0x72, 0x16, 0x00, 0x09, 0x61, 0x75, 0x74, 0x68, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32]), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
        Assert.IsTrue(packet.AssignedClientId.Span.SequenceEqual("0HMUPCOBPLN2K"u8));
        Assert.IsTrue(packet.AuthData.Span.SequenceEqual("auth-data"u8));
        Assert.IsTrue(packet.AuthMethod.Span.SequenceEqual("bearer"u8));
        Assert.AreEqual(2048u, packet.MaximumPacketSize);
        Assert.AreEqual(QoSLevel.QoS1, packet.MaximumQoS);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("some reason"u8));
        Assert.AreEqual(1024, packet.ReceiveMaximum);
        Assert.IsTrue(packet.ResponseInfo.Span.SequenceEqual("some response"u8));
        Assert.AreEqual(false, packet.RetainAvailable);
        Assert.AreEqual(180u, (ushort)packet.ServerKeepAlive);
        Assert.IsTrue(packet.ServerReference.Span.SequenceEqual("another-server"u8));
        Assert.AreEqual(3600u, packet.SessionExpiryInterval);
        Assert.AreEqual(false, packet.SharedSubscriptionAvailable);
        Assert.AreEqual(false, packet.SubscriptionIdentifiersAvailable);
        Assert.AreEqual(256, packet.TopicAliasMaximum);
        Assert.AreEqual(false, packet.WildcardSubscriptionAvailable);
        Assert.IsNotNull(packet.UserProperties);
        Assert.AreEqual(2, packet.UserProperties.Count);
        Assert.IsTrue(packet.UserProperties[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.UserProperties[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.UserProperties[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.UserProperties[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_ParseProps_GivenLargerContinuousSequenceWithProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x00, 0x02, 0x91, 0x01, 0x11, 0x00, 0x00, 0x0e, 0x10, 0x21, 0x04, 0x00, 0x24, 0x01, 0x25, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x12, 0x00, 0x0d, 0x30, 0x48, 0x4d, 0x55, 0x50, 0x43, 0x4f, 0x42, 0x50, 0x4c, 0x4e, 0x32, 0x4b, 0x22, 0x01, 0x00, 0x1f, 0x00, 0x0b, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x28, 0x00, 0x29, 0x00, 0x2a, 0x00, 0x13, 0x00, 0xb4, 0x1a, 0x00, 0x0d, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74, 0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x15, 0x00, 0x06, 0x62, 0x65, 0x61, 0x72, 0x65, 0x72, 0x16, 0x00, 0x09, 0x61, 0x75, 0x74, 0x68, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32, 0x00, 0x01, 0x02]), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
        Assert.IsTrue(packet.AssignedClientId.Span.SequenceEqual("0HMUPCOBPLN2K"u8));
        Assert.IsTrue(packet.AuthData.Span.SequenceEqual("auth-data"u8));
        Assert.IsTrue(packet.AuthMethod.Span.SequenceEqual("bearer"u8));
        Assert.AreEqual(2048u, packet.MaximumPacketSize);
        Assert.AreEqual(QoSLevel.QoS1, packet.MaximumQoS);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("some reason"u8));
        Assert.AreEqual(1024, packet.ReceiveMaximum);
        Assert.IsTrue(packet.ResponseInfo.Span.SequenceEqual("some response"u8));
        Assert.AreEqual(false, packet.RetainAvailable);
        Assert.AreEqual(180u, (ushort)packet.ServerKeepAlive);
        Assert.IsTrue(packet.ServerReference.Span.SequenceEqual("another-server"u8));
        Assert.AreEqual(3600u, packet.SessionExpiryInterval);
        Assert.AreEqual(false, packet.SharedSubscriptionAvailable);
        Assert.AreEqual(false, packet.SubscriptionIdentifiersAvailable);
        Assert.AreEqual(256, packet.TopicAliasMaximum);
        Assert.AreEqual(false, packet.WildcardSubscriptionAvailable);
        Assert.IsNotNull(packet.UserProperties);
        Assert.AreEqual(2, packet.UserProperties.Count);
        Assert.IsTrue(packet.UserProperties[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.UserProperties[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.UserProperties[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.UserProperties[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteContinuousSequence()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x00]), out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteContinuousSequenceWithProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(new([0x00, 0x02, 0x91, 0x01, 0x11, 0x00, 0x00, 0x0e, 0x10, 0x21, 0x04, 0x00, 0x24, 0x01, 0x25, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x12, 0x00, 0x0d, 0x30, 0x48, 0x4d, 0x55, 0x50, 0x43, 0x4f, 0x42, 0x50, 0x4c, 0x4e, 0x32, 0x4b, 0x22, 0x01, 0x00, 0x1f, 0x00, 0x0b, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x61, 0x73, 0x6f, 0x6e, 0x28, 0x00, 0x29, 0x00, 0x2a, 0x00, 0x13, 0x00, 0xb4, 0x1a, 0x00, 0x0d, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74, 0x68, 0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x15, 0x00, 0x06, 0x62, 0x65, 0x61, 0x72, 0x65, 0x72, 0x16, 0x00, 0x09, 0x61, 0x75, 0x74, 0x68, 0x2d, 0x64, 0x61, 0x74, 0x61, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00]), out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnTrue_ParseStatusCodeAndSessionPresent_GivenFragmentedSequence()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(SequenceFactory.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x02 }, new byte[] { 0x00 }), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);

        actual = Packets.V5.ConnAckPacket.TryReadPayload(SequenceFactory.Create<byte>(new byte[] { 0x01 }, new byte[] { 0x04 }, new byte[] { 0x00 }), out packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x04, packet.StatusCode);
        Assert.IsTrue(packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTrue_ParseStatusCodeAndSessionPresent_GivenLargerFragmentedSequence()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(SequenceFactory.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x02 }, new byte[] { 0x00, 0x00, 0x01, 0x02 }), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
    }

    [TestMethod]
    public void ReturnTrue_SetDefaultPropValues_GivenFragmentedSequenceWithNoProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(SequenceFactory.Create<byte>(new byte[] { 0x00 }, new byte[] { 0x02 }, new byte[] { 0x00 }), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
        Assert.AreEqual(null, packet.AssignedClientId);
        Assert.AreEqual(null, packet.AuthData);
        Assert.AreEqual(null, packet.AuthMethod);
        Assert.AreEqual(null, packet.MaximumPacketSize);
        Assert.AreEqual(QoSLevel.QoS2, packet.MaximumQoS);
        Assert.AreEqual(null, packet.ReasonString);
        Assert.AreEqual(ushort.MaxValue, packet.ReceiveMaximum);
        Assert.AreEqual(null, packet.ResponseInfo);
        Assert.AreEqual(true, packet.RetainAvailable);
        Assert.AreEqual(null, packet.ServerKeepAlive);
        Assert.AreEqual(null, packet.ServerReference);
        Assert.AreEqual(null, packet.SessionExpiryInterval);
        Assert.AreEqual(true, packet.SharedSubscriptionAvailable);
        Assert.AreEqual(true, packet.SubscriptionIdentifiersAvailable);
        Assert.AreEqual(true, packet.WildcardSubscriptionAvailable);
        Assert.AreEqual(0, packet.TopicAliasMaximum);
        Assert.IsNull(packet.UserProperties);
    }

    [TestMethod]
    public void ReturnTrue_ParseProps_GivenFragmentedSequenceWithProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(
            SequenceFactory.Create<byte>(
                new byte[] {
                    0x00, 0x02, 0x91, 0x01, 0x11, 0x00, 0x00, 0x0e, 0x10, 0x21, 0x04, 0x00, 0x24,
                    0x01, 0x25, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x12, 0x00, 0x0d, 0x30, 0x48,
                    0x4d, 0x55, 0x50, 0x43, 0x4f, 0x42, 0x50, 0x4c, 0x4e, 0x32, 0x4b, 0x22, 0x01
                },
                new byte[] {
                    0x00, 0x1f, 0x00, 0x0b, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x61, 0x73,
                    0x6f, 0x6e, 0x28, 0x00, 0x29, 0x00, 0x2a, 0x00, 0x13, 0x00, 0xb4, 0x1a, 0x00,
                    0x0d, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73
                },
                new byte[] {
                    0x65, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74, 0x68, 0x65, 0x72, 0x2d, 0x73,
                    0x65, 0x72, 0x76, 0x65, 0x72, 0x15, 0x00, 0x06, 0x62, 0x65, 0x61, 0x72, 0x65
                },
                new byte[] {
                    0x72, 0x16, 0x00, 0x09, 0x61, 0x75, 0x74, 0x68, 0x2d, 0x64, 0x61, 0x74, 0x61,
                    0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c
                },
                new byte[] {
                    0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76,
                    0x61, 0x6c, 0x75, 0x65, 0x32
                }), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
        Assert.IsTrue(packet.AssignedClientId.Span.SequenceEqual("0HMUPCOBPLN2K"u8));
        Assert.IsTrue(packet.AuthData.Span.SequenceEqual("auth-data"u8));
        Assert.IsTrue(packet.AuthMethod.Span.SequenceEqual("bearer"u8));
        Assert.AreEqual(2048u, packet.MaximumPacketSize);
        Assert.AreEqual(QoSLevel.QoS1, packet.MaximumQoS);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("some reason"u8));
        Assert.AreEqual(1024, packet.ReceiveMaximum);
        Assert.IsTrue(packet.ResponseInfo.Span.SequenceEqual("some response"u8));
        Assert.AreEqual(false, packet.RetainAvailable);
        Assert.AreEqual(180u, (ushort)packet.ServerKeepAlive);
        Assert.IsTrue(packet.ServerReference.Span.SequenceEqual("another-server"u8));
        Assert.AreEqual(3600u, packet.SessionExpiryInterval);
        Assert.AreEqual(false, packet.SharedSubscriptionAvailable);
        Assert.AreEqual(false, packet.SubscriptionIdentifiersAvailable);
        Assert.AreEqual(256, packet.TopicAliasMaximum);
        Assert.AreEqual(false, packet.WildcardSubscriptionAvailable);
        Assert.IsNotNull(packet.UserProperties);
        Assert.AreEqual(2, packet.UserProperties.Count);
        Assert.IsTrue(packet.UserProperties[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.UserProperties[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.UserProperties[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.UserProperties[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnTrue_ParseProps_GivenLargerFragmentedSequenceWithProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(
            SequenceFactory.Create<byte>(
                new byte[] {
                    0x00, 0x02, 0x91, 0x01, 0x11, 0x00, 0x00, 0x0e, 0x10, 0x21, 0x04, 0x00, 0x24,
                    0x01, 0x25, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x12, 0x00, 0x0d, 0x30, 0x48,
                    0x4d, 0x55, 0x50, 0x43, 0x4f, 0x42, 0x50, 0x4c, 0x4e, 0x32, 0x4b, 0x22, 0x01
                },
                new byte[] {
                    0x00, 0x1f, 0x00, 0x0b, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x61, 0x73,
                    0x6f, 0x6e, 0x28, 0x00, 0x29, 0x00, 0x2a, 0x00, 0x13, 0x00, 0xb4, 0x1a, 0x00,
                    0x0d, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73
                },
                new byte[] {
                    0x65, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74, 0x68, 0x65, 0x72, 0x2d, 0x73,
                    0x65, 0x72, 0x76, 0x65, 0x72, 0x15, 0x00, 0x06, 0x62, 0x65, 0x61, 0x72, 0x65
                },
                new byte[] {
                    0x72, 0x16, 0x00, 0x09, 0x61, 0x75, 0x74, 0x68, 0x2d, 0x64, 0x61, 0x74, 0x61,
                    0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c
                },
                new byte[] {
                    0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76,
                    0x61, 0x6c, 0x75, 0x65, 0x32, 0x00, 0x00, 0x01, 0x02
                }), out var packet);

        Assert.IsTrue(actual);
        Assert.IsNotNull(packet);
        Assert.AreEqual(0x02, packet.StatusCode);
        Assert.IsFalse(packet.SessionPresent);
        Assert.IsTrue(packet.AssignedClientId.Span.SequenceEqual("0HMUPCOBPLN2K"u8));
        Assert.IsTrue(packet.AuthData.Span.SequenceEqual("auth-data"u8));
        Assert.IsTrue(packet.AuthMethod.Span.SequenceEqual("bearer"u8));
        Assert.AreEqual(2048u, packet.MaximumPacketSize);
        Assert.AreEqual(QoSLevel.QoS1, packet.MaximumQoS);
        Assert.IsTrue(packet.ReasonString.Span.SequenceEqual("some reason"u8));
        Assert.AreEqual(1024, packet.ReceiveMaximum);
        Assert.IsTrue(packet.ResponseInfo.Span.SequenceEqual("some response"u8));
        Assert.AreEqual(false, packet.RetainAvailable);
        Assert.AreEqual(180u, (ushort)packet.ServerKeepAlive);
        Assert.IsTrue(packet.ServerReference.Span.SequenceEqual("another-server"u8));
        Assert.AreEqual(3600u, packet.SessionExpiryInterval);
        Assert.AreEqual(false, packet.SharedSubscriptionAvailable);
        Assert.AreEqual(false, packet.SubscriptionIdentifiersAvailable);
        Assert.AreEqual(256, packet.TopicAliasMaximum);
        Assert.AreEqual(false, packet.WildcardSubscriptionAvailable);
        Assert.IsNotNull(packet.UserProperties);
        Assert.AreEqual(2, packet.UserProperties.Count);
        Assert.IsTrue(packet.UserProperties[0].Name.Span.SequenceEqual("prop1"u8));
        Assert.IsTrue(packet.UserProperties[0].Value.Span.SequenceEqual("value1"u8));
        Assert.IsTrue(packet.UserProperties[1].Name.Span.SequenceEqual("prop2"u8));
        Assert.IsTrue(packet.UserProperties[1].Value.Span.SequenceEqual("value2"u8));
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteFragmentedSequence()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(SequenceFactory.Create<byte>(new byte[] { 0x00 }, Array.Empty<byte>(), Array.Empty<byte>()), out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }

    [TestMethod]
    public void ReturnFalse_PacketNull_GivenIncompleteFragmentedSequenceWithProps()
    {
        var actual = Packets.V5.ConnAckPacket.TryReadPayload(
            SequenceFactory.Create<byte>(
                new byte[] {
                    0x00, 0x02, 0x91, 0x01, 0x11, 0x00, 0x00, 0x0e, 0x10, 0x21, 0x04, 0x00, 0x24,
                    0x01, 0x25, 0x00, 0x27, 0x00, 0x00, 0x08, 0x00, 0x12, 0x00, 0x0d, 0x30, 0x48,
                    0x4d, 0x55, 0x50, 0x43, 0x4f, 0x42, 0x50, 0x4c, 0x4e, 0x32, 0x4b, 0x22, 0x01
                },
                new byte[] {
                    0x00, 0x1f, 0x00, 0x0b, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x61, 0x73,
                    0x6f, 0x6e, 0x28, 0x00, 0x29, 0x00, 0x2a, 0x00, 0x13, 0x00, 0xb4, 0x1a, 0x00,
                    0x0d, 0x73, 0x6f, 0x6d, 0x65, 0x20, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73
                },
                new byte[] {
                    0x65, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74, 0x68, 0x65, 0x72, 0x2d, 0x73,
                    0x65, 0x72, 0x76, 0x65, 0x72, 0x15, 0x00, 0x06, 0x62, 0x65, 0x61, 0x72, 0x65
                },
                new byte[] {
                    0x72, 0x16, 0x00, 0x09, 0x61, 0x75, 0x74, 0x68, 0x2d, 0x64, 0x61, 0x74, 0x61,
                    0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c
                }), out var packet);

        Assert.IsFalse(actual);
        Assert.IsNull(packet);
    }
}