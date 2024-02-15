using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V5.ConnectPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void EncodeFixedHeader_GivenSamplePacket()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        Packets.V5.ConnectPacket connectPacket = new(cleanStart: false);
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        Assert.AreEqual(PacketFlags.ConnectMask, bytes[0]);
        Assert.AreEqual(13, bytes[1]);
    }

    [TestMethod]
    public void EncodeVariableHeader_GivenSamplePacket()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        Packets.V5.ConnectPacket connectPacket = new(keepAlive: 120, cleanStart: false);
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        // 'MQTT' protocol name as UTF-8 string
        Assert.IsTrue(bytes[2..8].SequenceEqual((ReadOnlySpan<byte>)[0x00, 0x04, 0x4d, 0x51, 0x54, 0x54]));
        // protocol version 5
        Assert.AreEqual(0x05, bytes[8]);
        // CONNECT flags
        Assert.AreEqual(0x00, bytes[9]);
        // KeepAlive value bytes
        Assert.IsTrue(bytes[10..12].SequenceEqual((ReadOnlySpan<byte>)[0, 120]));
        // Connect properties length
        Assert.AreEqual(0, bytes[12]);
    }

    [TestMethod]
    public void EncodeVariableHeaderConnectFlags_GivenPacketWithNonEmptyProperties()
    {
        var writer = new ArrayBufferWriter<byte>(78);
        Packets.V5.ConnectPacket connectPacket = new(
            userName: "test-user"u8.ToArray(), password: "test-password"u8.ToArray(),
            willTopic: "test-will-topic"u8.ToArray(), willPayload: "test-will-payload"u8.ToArray(),
            willQoS: QoSLevel.QoS1, willRetain: true);
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(78, written);
        Assert.AreEqual(78, writer.WrittenCount);

        // CONNECT flags
        var actual = bytes[9];
        Assert.AreEqual(0b1110_1110, actual);
    }

    [TestMethod]
    public void EncodeSessionExpiryInterval_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(20);
        Packets.V5.ConnectPacket connectPacket = new() { SessionExpiryInterval = 300 };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(20, written);
        Assert.AreEqual(20, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x05, 0x11, 0x00, 0x00, 0x01, 0x2c]));
    }

    [TestMethod]
    public void EncodeReceiveMaximum_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(18);
        Packets.V5.ConnectPacket connectPacket = new() { ReceiveMaximum = 2048 };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(18, written);
        Assert.AreEqual(18, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x03, 0x21, 0x08, 0x00]));
    }

    [TestMethod]
    public void EncodeMaximumPacketSize_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(20);
        Packets.V5.ConnectPacket connectPacket = new() { MaximumPacketSize = ushort.MaxValue };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(20, written);
        Assert.AreEqual(20, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x05, 0x27, 0x00, 0x00, 0xff, 0xff]));
    }

    [TestMethod]
    public void EncodeTopicAliasMaximum_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(18);
        Packets.V5.ConnectPacket connectPacket = new() { TopicAliasMaximum = 1024 };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(18, written);
        Assert.AreEqual(18, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x03, 0x22, 0x04, 0x00]));
    }

    [TestMethod]
    public void EncodeRequestResponseInformation_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(17);
        Packets.V5.ConnectPacket connectPacket = new() { RequestResponse = true };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(17, written);
        Assert.AreEqual(17, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x02, 0x19, 0x01]));
    }

    [TestMethod]
    public void EncodeRequestProblemInformation_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(17);
        Packets.V5.ConnectPacket connectPacket = new() { RequestProblem = false };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(17, written);
        Assert.AreEqual(17, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x02, 0x17, 0x00]));
    }

    [TestMethod]
    public void EncodeAuthenticationMethod_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(24);
        Packets.V5.ConnectPacket connectPacket = new() { AuthenticationMethod = "Bearer"u8.ToArray() };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(24, written);
        Assert.AreEqual(24, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x09, 0x15, 0x00, 0x06, 0x42, 0x65, 0x61, 0x72, 0x65, 0x72]));
    }

    [TestMethod]
    public void EncodeAuthenticationData_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(24);
        Packets.V5.ConnectPacket connectPacket = new() { AuthenticationData = "abcdef"u8.ToArray() };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(24, written);
        Assert.AreEqual(24, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x09, 0x16, 0x00, 0x06, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66]));
    }

    [TestMethod]
    public void EncodeUserProperties_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        Packets.V5.ConnectPacket connectPacket = new()
        {
            UserProperties = [("prop1"u8.ToArray(), "value1"u8.ToArray()), ("prop2"u8.ToArray(), "value2"u8.ToArray())]
        };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        Assert.IsTrue(bytes[12..^2].SequenceEqual((byte[])[0x20, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32]));
    }

    [TestMethod]
    public void EncodePropertiesZeroLength_GivenPacketWithAllDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        Packets.V5.ConnectPacket connectPacket = new();
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        Assert.IsTrue(bytes[12..13].SequenceEqual((byte[])[0x00]));
    }

    [TestMethod]
    public void EncodeClientId_GivenPacketWithNonEmptyValue()
    {
        var writer = new ArrayBufferWriter<byte>(28);
        Packets.V5.ConnectPacket connectPacket = new(clientId: "mqtt_619c7deb"u8.ToArray());
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(28, written);
        Assert.AreEqual(28, writer.WrittenCount);

        Assert.IsTrue(bytes[13..].SequenceEqual((byte[])[0x00, 0x0d, 0x6d, 0x71, 0x74, 0x74, 0x5f, 0x36, 0x31, 0x39, 0x63, 0x37, 0x64, 0x65, 0x62]));
    }

    [TestMethod]
    public void EncodeClientIdZeroLength_GivenPacketWithEmptyValue()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        Packets.V5.ConnectPacket connectPacket = new(clientId: default);
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        Assert.IsTrue(bytes[13..].SequenceEqual((byte[])[0x00, 0x00]));
    }

    [TestMethod]
    public void EncodeWillDelayInterval_GivenPacketWithNonDefaultValueAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(35);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray()) { WillDelayInterval = 120 };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(35, written);
        Assert.AreEqual(35, writer.WrittenCount);

        Assert.IsTrue(bytes[15..21].SequenceEqual((byte[])[0x05, 0x18, 0x00, 0x00, 0x00, 0x78]));
    }

    [TestMethod]
    public void EncodeWillPayloadFormatIndicator_GivenPacketWithNonDefaultValueAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(32);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray()) { WillPayloadFormat = true };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(32, written);
        Assert.AreEqual(32, writer.WrittenCount);

        Assert.IsTrue(bytes[15..18].SequenceEqual((byte[])[0x02, 0x01, 0x01]));
    }

    [TestMethod]
    public void EncodeWillExpiryInterval_GivenPacketWithNonDefaultValueAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(35);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray()) { WillExpiryInterval = 3600 };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(35, written);
        Assert.AreEqual(35, writer.WrittenCount);

        Assert.IsTrue(bytes[15..21].SequenceEqual((byte[])[0x05, 0x02, 0x00, 0x00, 0x0e, 0x10]));
    }

    [TestMethod]
    public void EncodeWillContentType_GivenPacketWithNonDefaultValueAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(42);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray()) { WillContentType = "text/json"u8.ToArray() };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(42, written);
        Assert.AreEqual(42, writer.WrittenCount);

        Assert.IsTrue(bytes[15..28].SequenceEqual((byte[])[0x0c, 0x03, 0x00, 0x09, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x6a, 0x73, 0x6f, 0x6e]));
    }

    [TestMethod]
    public void EncodeWillResponseTopic_GivenPacketWithNonDefaultValueAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(41);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray()) { WillResponseTopic = "response"u8.ToArray() };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(41, written);
        Assert.AreEqual(41, writer.WrittenCount);

        Assert.IsTrue(bytes[15..27].SequenceEqual((byte[])[0x0b, 0x08, 0x00, 0x08, 0x72, 0x65, 0x73, 0x70, 0x6f, 0x6e, 0x73, 0x65]));
    }

    [TestMethod]
    public void EncodeWillCorrelationData_GivenPacketWithNonDefaultValueAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(37);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray()) { WillCorrelationData = "data"u8.ToArray() };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(37, written);
        Assert.AreEqual(37, writer.WrittenCount);

        Assert.IsTrue(bytes[15..23].SequenceEqual((byte[])[0x07, 0x09, 0x00, 0x04, 0x64, 0x61, 0x74, 0x61]));
    }

    [TestMethod]
    public void EncodeWillUserProperties_GivenPacketWithNonDefaultValueAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(62);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray())
        {
            WillUserProperties = [("prop1"u8.ToArray(), "value1"u8.ToArray()), ("prop2"u8.ToArray(), "value2"u8.ToArray())]
        };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(62, written);
        Assert.AreEqual(62, writer.WrittenCount);

        Assert.IsTrue(bytes[15..48].SequenceEqual((byte[])[0x20, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32]));
    }

    [TestMethod]
    public void EncodeWillPropertiesZeroLength_GivenPacketWithAllDefaultValuesAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(30);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray());
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(30, written);
        Assert.AreEqual(30, writer.WrittenCount);

        Assert.IsTrue(bytes[15..16].SequenceEqual((byte[])[0x00]));
    }

    [TestMethod]
    public void EncodeWillTopic_GivenPacketWithWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(30);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray());
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(30, written);
        Assert.AreEqual(30, writer.WrittenCount);

        Assert.IsTrue(bytes[16..28].SequenceEqual((byte[])[0x00, 0x0a, 0x77, 0x69, 0x6c, 0x6c, 0x2d, 0x74, 0x6f, 0x70, 0x69, 0x63]));
    }

    [TestMethod]
    public void EncodeWillPayload_GivenPacketWithWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(39);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray(), willPayload: "will-data"u8.ToArray());
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(39, written);
        Assert.AreEqual(39, writer.WrittenCount);

        Assert.IsTrue(bytes[28..].SequenceEqual((byte[])[0x00, 0x09, 0x77, 0x69, 0x6c, 0x6c, 0x2d, 0x64, 0x61, 0x74, 0x61]));
    }

    [TestMethod]
    public void EncodeWillPayloadZeroLength_GivenPacketWithEmptyPayloadAndWillTopicPresent()
    {
        var writer = new ArrayBufferWriter<byte>(30);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: "will-topic"u8.ToArray());
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(30, written);
        Assert.AreEqual(30, writer.WrittenCount);

        Assert.IsTrue(bytes[28..].SequenceEqual((byte[])[0x00, 0x00]));
    }

    [TestMethod]
    public void DoNotEncodeWillPropertiesAndWillTopicAndWillPayload_GivenPacketWithEmptyWillTopic()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        Packets.V5.ConnectPacket connectPacket = new(willTopic: default, willPayload: "will-data"u8.ToArray())
        {
            WillDelayInterval = 120,
            WillPayloadFormat = true,
            WillExpiryInterval = 3600,
            WillContentType = "text/json"u8.ToArray(),
            WillResponseTopic = "response"u8.ToArray(),
            WillCorrelationData = "data"u8.ToArray(),
            WillUserProperties = [("prop1"u8.ToArray(), "value1"u8.ToArray()), ("prop2"u8.ToArray(), "value2"u8.ToArray())]
        };
        var written = connectPacket.Write(writer);

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);
    }

    [TestMethod]
    public void EncodeUserName_GivenPacketWithUserNamePresent()
    {
        var writer = new ArrayBufferWriter<byte>(26);
        Packets.V5.ConnectPacket connectPacket = new(userName: "test-user"u8.ToArray());
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(26, written);
        Assert.AreEqual(26, writer.WrittenCount);

        Assert.AreEqual(PacketFlags.UserNameMask, bytes[9] & PacketFlags.UserNameMask);
        Assert.IsTrue(bytes[15..].SequenceEqual((byte[])[0x00, 0x09, 0x74, 0x65, 0x73, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72]));
    }

    [TestMethod]
    public void DoNotEncodeUserName_GivenPacketWithUserNameEmpty()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        Packets.V5.ConnectPacket connectPacket = new(userName: default);
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        Assert.AreEqual(0, bytes[9] & PacketFlags.UserNameMask);
    }

    [TestMethod]
    public void EncodePassword_GivenPacketWithPasswordPresent()
    {
        var writer = new ArrayBufferWriter<byte>(25);
        Packets.V5.ConnectPacket connectPacket = new(password: "test-pwd"u8.ToArray());
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(25, written);
        Assert.AreEqual(25, writer.WrittenCount);

        Assert.AreEqual(PacketFlags.PasswordMask, bytes[9] & PacketFlags.PasswordMask);
        Assert.IsTrue(bytes[15..].SequenceEqual((byte[])[0x00, 0x08, 0x74, 0x65, 0x73, 0x74, 0x2d, 0x70, 0x77, 0x64]));
    }

    [TestMethod]
    public void DoNotEncodePassword_GivenPacketWithPasswordEmpty()
    {
        var writer = new ArrayBufferWriter<byte>(15);
        Packets.V5.ConnectPacket connectPacket = new(password: default);
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(15, written);
        Assert.AreEqual(15, writer.WrittenCount);

        Assert.AreEqual(0, bytes[9] & PacketFlags.PasswordMask);
    }

    [TestMethod]
    public void EncodePayloadPartsInStrictOrder()
    {
        var writer = new ArrayBufferWriter<byte>(85);
        Packets.V5.ConnectPacket connectPacket = new(
            clientId: "mqtt_619c7deb"u8.ToArray(),
            willTopic: "will-topic"u8.ToArray(),
            willPayload: "will-data"u8.ToArray(),
            userName: "test-user"u8.ToArray(),
            password: "test-pwd"u8.ToArray())
        {
            WillContentType = "text/json"u8.ToArray()
        };
        var written = connectPacket.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(85, written);
        Assert.AreEqual(85, writer.WrittenCount);

        Assert.IsTrue(bytes[13..28].SequenceEqual((byte[])[0x00, 0x0d, 0x6d, 0x71, 0x74, 0x74, 0x5f, 0x36, 0x31, 0x39, 0x63, 0x37, 0x64, 0x65, 0x62]));
        Assert.IsTrue(bytes[28..41].SequenceEqual((byte[])[0x0c, 0x03, 0x00, 0x09, 0x74, 0x65, 0x78, 0x74, 0x2f, 0x6a, 0x73, 0x6f, 0x6e]));
        Assert.IsTrue(bytes[41..53].SequenceEqual((byte[])[0x00, 0x0a, 0x77, 0x69, 0x6c, 0x6c, 0x2d, 0x74, 0x6f, 0x70, 0x69, 0x63]));
        Assert.IsTrue(bytes[53..64].SequenceEqual((byte[])[0x00, 0x09, 0x77, 0x69, 0x6c, 0x6c, 0x2d, 0x64, 0x61, 0x74, 0x61]));
        Assert.IsTrue(bytes[64..75].SequenceEqual((byte[])[0x00, 0x09, 0x74, 0x65, 0x73, 0x74, 0x2d, 0x75, 0x73, 0x65, 0x72]));
        Assert.IsTrue(bytes[75..].SequenceEqual((byte[])[0x00, 0x08, 0x74, 0x65, 0x73, 0x74, 0x2d, 0x70, 0x77, 0x64]));
    }
}
