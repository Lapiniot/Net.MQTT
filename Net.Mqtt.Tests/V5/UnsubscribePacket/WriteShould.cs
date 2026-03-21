using System.Buffers.Binary;

namespace Net.Mqtt.Tests.V5.UnsubscribePacket;

[TestClass]
public class WriteShould
{
    private readonly Packets.V5.UnsubscribePacket unsubscribePacket = new(0x0002, ["testtopic0/#"u8.ToArray(), "testtopic1/#"u8.ToArray(), "testtopic2/#"u8.ToArray()]);

    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = unsubscribePacket.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual((byte)(0b1010_0000 | 0b0010), actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(45, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePacketId_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = unsubscribePacket.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual((byte)0x0002, actualPacketId);
    }

    [TestMethod]
    public void EncodeTopics_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = unsubscribePacket.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        Assert.AreEqual(12, BinaryPrimitives.ReadUInt16BigEndian(bytes[5..]));
        CollectionAssert.AreEqual("testtopic0/#"u8, bytes.Slice(7, 12));

        Assert.AreEqual(12, BinaryPrimitives.ReadUInt16BigEndian(bytes[19..]));
        CollectionAssert.AreEqual("testtopic1/#"u8, bytes.Slice(21, 12));

        Assert.AreEqual(12, BinaryPrimitives.ReadUInt16BigEndian(bytes[33..]));
        CollectionAssert.AreEqual("testtopic2/#"u8, bytes.Slice(35, 12));
    }

    [TestMethod]
    public void EncodeUserProperties_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(51);
        var written = new Packets.V5.UnsubscribePacket(0x0002, ["testtopic0/#"u8.ToArray()])
        {
            UserProperties =
            [
                ("prop1"u8.ToArray(), "value1"u8.ToArray()),
                ("prop2"u8.ToArray(), "value2"u8.ToArray())
            ]
        }.Write(writer, int.MaxValue);
        var bytes = writer.WrittenSpan;

        Assert.AreEqual(51, written);
        Assert.AreEqual(51, writer.WrittenCount);

        Assert.AreEqual(32, bytes[4]);

        Assert.AreEqual(0x26, bytes[5]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes[6..]));
        CollectionAssert.AreEqual("prop1"u8, bytes.Slice(8, 5));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes[13..]));
        CollectionAssert.AreEqual("value1"u8, bytes.Slice(15, 6));

        Assert.AreEqual(0x26, bytes[21]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes[22..]));
        CollectionAssert.AreEqual("prop2"u8, bytes.Slice(24, 5));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes[29..]));
        CollectionAssert.AreEqual("value2"u8, bytes.Slice(31, 6));
    }
}