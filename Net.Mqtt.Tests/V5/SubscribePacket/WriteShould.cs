using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V5.SubscribePacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void EncodeFixedHeader_GivenSamplePacket()
    {
        var writer = new ArrayBufferWriter<byte>(20);
        var written = new Packets.V5.SubscribePacket(0x0001, [("testtopic0/#"u8.ToArray(), 0)]).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(20, written);
        Assert.AreEqual(20, writer.WrittenCount);

        // Header marker
        Assert.AreEqual(PacketFlags.SubscribeMask, bytes[0]);
        // Remaining length
        Assert.AreEqual(18, bytes[1]);
    }

    [TestMethod]
    public void EncodeVariableHeader_GivenSamplePacket()
    {
        var writer = new ArrayBufferWriter<byte>(20);
        var written = new Packets.V5.SubscribePacket(0x9fd6, [("testtopic0/#"u8.ToArray(), 0)]).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(20, written);
        Assert.AreEqual(20, writer.WrittenCount);

        // Packet Id
        Assert.AreEqual(0x9fd6, BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]));
        // Properties length
        Assert.AreEqual(0x00, bytes[4]);
    }

    [TestMethod]
    public void EncodeSubscriptionIdentifier_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(23);
        var written = new Packets.V5.SubscribePacket(0xe5d6, [("testtopic0/#"u8.ToArray(), 0)]) { SubscriptionIdentifier = 2048 }.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(23, written);
        Assert.AreEqual(23, writer.WrittenCount);

        Assert.IsTrue(bytes[4..8].SequenceEqual((byte[])[0x03, 0x0b, 0x80, 0x10]));
    }

    [TestMethod]
    public void EncodeUserProperties_GivenPacketWithNonDefaultValue()
    {
        var writer = new ArrayBufferWriter<byte>(52);
        var written = new Packets.V5.SubscribePacket(0xe5d6, [("testtopic0/#"u8.ToArray(), 0)])
        {
            UserProperties = [("prop1"u8.ToArray(), "value1"u8.ToArray()), ("prop2"u8.ToArray(), "value2"u8.ToArray())]
        }.Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(52, written);
        Assert.AreEqual(52, writer.WrittenCount);

        Assert.IsTrue(bytes[4..37].SequenceEqual((byte[])[0x20, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x31, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x31, 0x26, 0x00, 0x05, 0x70, 0x72, 0x6f, 0x70, 0x32, 0x00, 0x06, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x32]));
    }

    [TestMethod]
    public void EncodePayload_GivenSamplePacket()
    {
        var writer = new ArrayBufferWriter<byte>(50);
        var written = new Packets.V5.SubscribePacket(0xe5d6, [
            ("testtopic0/#"u8.ToArray(), 0b0000_0000),
            ("testtopic1/#"u8.ToArray(), 0b0010_0001),
            ("testtopic2/#"u8.ToArray(), 0b0001_1110)
        ]).Write(writer);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(50, written);
        Assert.AreEqual(50, writer.WrittenCount);

        Assert.IsTrue(bytes[5..20].SequenceEqual((byte[])[0x00, 0x0c, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x30, 0x2f, 0x23, 0x00]));
        Assert.IsTrue(bytes[20..35].SequenceEqual((byte[])[0x00, 0x0c, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x31, 0x2f, 0x23, 0x21]));
        Assert.IsTrue(bytes[35..].SequenceEqual((byte[])[0x00, 0x0c, 0x74, 0x65, 0x73, 0x74, 0x74, 0x6f, 0x70, 0x69, 0x63, 0x32, 0x2f, 0x23, 0x1e]));
    }
}