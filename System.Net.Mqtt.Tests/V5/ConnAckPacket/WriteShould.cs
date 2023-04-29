using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.ConnAckPacket;

[TestClass]
public record class WriteShould
{
    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(5);
        var written = new Packets.V5.ConnAckPacket(0x02, true).Write(writer, out var bytes);

        Assert.AreEqual(5, written);
        Assert.AreEqual(5, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(0b100000, actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(0x03, actualRemainingLength);

        var actualPropsLength = bytes[4];
        Assert.AreEqual(0x00, actualPropsLength);
    }

    [TestMethod]
    public void EncodeResultBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(5);
        var written = new Packets.V5.ConnAckPacket(0x02, true).Write(writer, out var bytes);

        Assert.AreEqual(5, written);
        Assert.AreEqual(5, writer.WrittenCount);

        Assert.AreEqual(0x1, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);

        writer.Clear();
        written = new Packets.V5.ConnAckPacket(0x02, false).Write(writer, out bytes);

        Assert.AreEqual(5, written);
        Assert.AreEqual(5, writer.WrittenCount);

        Assert.AreEqual(0x0, bytes[2]);
        Assert.AreEqual(0x2, bytes[3]);
    }

    [TestMethod]
    public void EncodeSessionExpiryInterval_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(10);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { SessionExpiryInterval = 300 };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(10, written);
        Assert.AreEqual(10, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x08, 0x01, 0x02, 0x05, 0x11, 0x00, 0x00, 0x01, 0x2c }));
    }

    [TestMethod]
    public void EncodeReceiveMaximum_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(8);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { ReceiveMaximum = 0x400 };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x06, 0x01, 0x02, 0x03, 0x21, 0x04, 0x00 }));
    }

    [TestMethod]
    public void EncodeMaximumQoS_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(7);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { MaximumQoS = QoSLevel.QoS1 };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x05, 0x01, 0x02, 0x02, 0x24, 0x01 }));
    }

    [TestMethod]
    public void EncodeRetainAvailable_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(7);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { RetainAvailable = false };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x05, 0x01, 0x02, 0x02, 0x25, 0x00 }));
    }

    [TestMethod]
    public void EncodeMaximumPacketSize_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(10);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { MaximumPacketSize = 0x1000 };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(10, written);
        Assert.AreEqual(10, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x08, 0x01, 0x02, 0x05, 0x27, 0x00, 0x00, 0x10, 0x00 }));
    }

    [TestMethod]
    public void EncodeAssignedClientId_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(22);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { AssignedClientId = "mqttx_a6438c55"u8.ToArray() };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(22, written);
        Assert.AreEqual(22, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x20, 0x14, 0x01, 0x02, 0x11, 0x12, 0x00, 0x0e, 0x6d, 0x71, 0x74,
            0x74, 0x78, 0x5f, 0x61, 0x36, 0x34, 0x33, 0x38, 0x63, 0x35, 0x35 }));
    }

    [TestMethod]
    public void EncodeTopicAliasMaximum_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(8);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { TopicAliasMaximum = 0x200 };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x06, 0x01, 0x02, 0x03, 0x22, 0x02, 0x00 }));
    }

    [TestMethod]
    public void EncodeReasonString_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(25);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { ReasonString = "Invalid client id"u8.ToArray() };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x20, 0x17, 0x01, 0x02, 0x14, 0x1f, 0x00, 0x11, 0x49, 0x6e, 0x76, 0x61, 0x6c,
            0x69, 0x64, 0x20, 0x63, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x20, 0x69, 0x64 }));
    }

    [TestMethod]
    public void EncodeProperties_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(69);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true)
        {
            Properties = new List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)>()
            {
                new("user-prop-1"u8.ToArray(),"user-prop1-value"u8.ToArray()),
                new("user-prop-2"u8.ToArray(),"user-prop2-value"u8.ToArray())
            }
        };

        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(69, written);
        Assert.AreEqual(69, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x20, 0x43, 0x01, 0x02, 0x40, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70,
            0x72, 0x6f, 0x70, 0x2d, 0x31, 0x00, 0x10, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72,
            0x6f, 0x70, 0x31, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x26, 0x00, 0x0b, 0x75, 0x73,
            0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x2d, 0x32, 0x00, 0x10, 0x75, 0x73, 0x65,
            0x72, 0x2d, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65 }));
    }

    [TestMethod]
    public void EncodeWildcardSubscriptionAvailable_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(7);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { WildcardSubscriptionAvailable = false };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x05, 0x01, 0x02, 0x02, 0x28, 0x00 }));
    }

    [TestMethod]
    public void EncodeSubscriptionIdentifiersAvailable_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(7);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { SubscriptionIdentifiersAvailable = false };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x05, 0x01, 0x02, 0x02, 0x29, 0x00 }));
    }

    [TestMethod]
    public void EncodeSharedSubscriptionAvailable_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(7);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { SharedSubscriptionAvailable = false };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(7, written);
        Assert.AreEqual(7, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 0x20, 0x05, 0x01, 0x02, 0x02, 0x2a, 0x00 }));
    }

    [TestMethod]
    public void EncodeServerKeepAlive_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(8);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { ServerKeepAlive = 0x78 };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(8, written);
        Assert.AreEqual(8, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] { 32, 6, 1, 2, 3, 19, 0, 120 }));
    }

    [TestMethod]
    public void EncodeResponseInfo_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(21);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { ResponseInfo = "Response info"u8.ToArray() };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(21, written);
        Assert.AreEqual(21, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x20, 0x13, 0x01, 0x02, 0x10, 0x1a, 0x00, 0x0d, 0x52, 0x65, 0x73,
            0x70, 0x6f, 0x6e, 0x73, 0x65, 0x20, 0x69, 0x6e, 0x66, 0x6f }));
    }

    [TestMethod]
    public void EncodeServerReference_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(17);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { ServerReference = "Server #1"u8.ToArray() };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(17, written);
        Assert.AreEqual(17, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x20, 0x0f, 0x01, 0x02, 0x0c, 0x1c, 0x00, 0x09, 0x53,
            0x65, 0x72, 0x76, 0x65, 0x72, 0x20, 0x23, 0x31 }));
    }

    [TestMethod]
    public void EncodeAuthMethod_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(14);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { AuthMethod = "Bearer"u8.ToArray() };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(14, written);
        Assert.AreEqual(14, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x20, 0x0c, 0x01, 0x02, 0x09, 0x15, 0x00,
            0x06, 0x42, 0x65, 0x61, 0x72, 0x65, 0x72
        }));
    }

    [TestMethod]
    public void EncodeAuthData_GivenNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(25);

        var connAckPacket = new Packets.V5.ConnAckPacket(0x02, true) { AuthData = "72f26a1a-337ac7cf"u8.ToArray() };
        var written = connAckPacket.Write(writer, out var bytes);

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        Assert.IsTrue(bytes.SequenceEqual(new byte[] {
            0x20, 0x17, 0x01, 0x02, 0x14, 0x16, 0x00, 0x11, 0x37, 0x32, 0x66, 0x32, 0x36,
            0x61, 0x31, 0x61, 0x2d, 0x33, 0x33, 0x37, 0x61, 0x63, 0x37, 0x63, 0x66 }));
    }
}